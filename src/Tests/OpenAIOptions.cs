using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devlooped.Extensions.AI;

record OpenAIOptions(string Key, string[] Vectors)
{
    public static OpenAIOptions Empty { get; } = new();

    public OpenAIOptions() : this("", []) { }
}