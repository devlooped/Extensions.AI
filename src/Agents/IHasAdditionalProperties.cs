using Microsoft.Extensions.AI;

namespace Devlooped.Agents.AI;

/// <summary>Indicates that the instance can have additional properties associated with it.</summary>
public interface IHasAdditionalProperties
{
    /// <summary>Gets or sets any additional properties associated with the instance.</summary>
    AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
