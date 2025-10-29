using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Configuration;
using NetEscapades.Configuration.Yaml;

namespace Devlooped.Agents.AI;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class AgentMarkdownExtensions
{
    /// <summary>
    /// Adds an instructions markdown file with optional YAML front-matter to the configuration sources.
    /// </summary>
    public static IConfigurationBuilder AddAgentMarkdown(this IConfigurationBuilder builder, string path, bool optional = false, bool reloadOnChange = false)
        => builder.Add<InstructionsConfigurationSource>(source =>
        {
            source.Path = path;
            source.Optional = optional;
            source.ReloadOnChange = reloadOnChange;
            source.ResolveFileProvider();
        });

    /// <summary>
    /// Adds an instructions markdown stream with optional YAML front-matter to the configuration sources.
    /// </summary>
    public static IConfigurationBuilder AddAgentMarkdown(this IConfigurationBuilder builder, Stream stream)
        => Throw.IfNull(builder).Add((InstructionsStreamConfigurationSource source) => source.Stream = stream);

    static class InstructionsParser
    {
        public static Dictionary<string, string?> Parse(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var frontMatter = new StringBuilder();
            var line = reader.ReadLine();
            // First line must be the front-matter according to spec.
            if (line == "---")
            {
                while ((line = reader.ReadLine()) != "---" && !reader.EndOfStream)
                    frontMatter.AppendLine(line);
            }

            if (frontMatter.Length > 0 && line != "---")
                throw new FormatException("Instructions markdown front-matter is not properly closed with '---'.");

            var instructions = reader.ReadToEnd().Trim();
            var data = new YamlConfigurationStreamParser().Parse(new MemoryStream(Encoding.UTF8.GetBytes(frontMatter.ToString())));
            if (!data.TryGetValue("id", out var value) || Convert.ToString(value) is not { Length: > 1 } id)
                throw new FormatException("Instructions markdown file must contain YAML front-matter with an 'id' key that specifies the section identifier.");

            data.Remove("id");
            // id should use the config delimiter rather than dot (which is a typical mistake when coming from TOML)
            id = id.Replace(".", ConfigurationPath.KeyDelimiter);
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in data)
                result[$"{id}{ConfigurationPath.KeyDelimiter}{entry.Key}"] = entry.Value;

            result[$"{id}{ConfigurationPath.KeyDelimiter}instructions"] = instructions;
            return result;
        }
    }

    class InstructionsStreamConfigurationSource : StreamConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder) => new InstructionsStreamConfigurationProvider(this);
    }

    class InstructionsStreamConfigurationProvider(InstructionsStreamConfigurationSource source) : StreamConfigurationProvider(source)
    {
        public override void Load(Stream stream) => Data = InstructionsParser.Parse(stream);
    }

    class InstructionsConfigurationSource : FileConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new InstructionsConfigurationProvider(this);
        }
    }

    class InstructionsConfigurationProvider(FileConfigurationSource source) : FileConfigurationProvider(source)
    {
        public override void Load(Stream stream) => Data = InstructionsParser.Parse(stream);
    }
}
