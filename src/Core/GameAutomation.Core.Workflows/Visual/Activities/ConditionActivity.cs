using GameAutomation.Core.Workflows.Visual.Models;

namespace GameAutomation.Core.Workflows.Visual.Activities;

/// <summary>
/// Conditional branching based on variable or expression.
/// Routes execution to True or False output port.
/// </summary>
public class ConditionActivity : IActivity
{
    public string Type => ActivityTypes.Condition;
    public string DisplayName => "Condition";
    public string Category => "Flow";
    public string Description => "Branch based on a condition";
    public string Icon => "CallSplit";

    public IReadOnlyList<Port> GetInputPorts() => new[]
    {
        Port.FlowInput("In"),
        Port.DataInput("Value", DataType.Any)
    };

    public IReadOnlyList<Port> GetOutputPorts() => new[]
    {
        Port.FlowOutput("True"),
        Port.FlowOutput("False")
    };

    public IReadOnlyList<ActivityPropertyDefinition> GetPropertyDefinitions() => new[]
    {
        new ActivityPropertyDefinition("VariableName", "Variable Name", PropertyType.String)
        {
            Description = "Name of variable to check"
        },
        new ActivityPropertyDefinition("Operator", "Comparison", PropertyType.Enum)
        {
            DefaultValue = "Equals",
            Options = new[]
            {
                "Exists",       // Variable exists and is not null
                "NotExists",    // Variable doesn't exist or is null
                "Equals",       // ==
                "NotEquals",    // !=
                "GreaterThan",  // >
                "LessThan",     // <
                "Contains",     // String contains
                "StartsWith",   // String starts with
                "EndsWith"      // String ends with
            },
            Description = "Type of comparison"
        },
        new ActivityPropertyDefinition("CompareValue", "Compare Value", PropertyType.String)
        {
            Description = "Value to compare against"
        }
    };

    public Task<ActivityResult> ExecuteAsync(
        IActivityContext context,
        ActivityNode node,
        IReadOnlyDictionary<string, object?> inputValues)
    {
        var variableName = node.GetProperty<string>("VariableName");
        var op = node.GetProperty("Operator", "Exists");
        var compareValue = node.GetProperty<string>("CompareValue");

        // Get value from input port or variable
        object? value;
        if (inputValues.TryGetValue("Value", out var inputValue))
        {
            value = inputValue;
        }
        else if (!string.IsNullOrEmpty(variableName))
        {
            context.Variables.TryGetValue(variableName, out value);
        }
        else
        {
            return Task.FromResult(ActivityResult.Fail("No value or variable specified"));
        }

        var result = EvaluateCondition(value, op, compareValue);
        context.Log($"Condition evaluated: {result}");

        return Task.FromResult(ActivityResult.Branch(result));
    }

    private static bool EvaluateCondition(object? value, string op, string? compareValue)
    {
        return op switch
        {
            "Exists" => value != null,
            "NotExists" => value == null,
            "Equals" => CompareEquals(value, compareValue),
            "NotEquals" => !CompareEquals(value, compareValue),
            "GreaterThan" => CompareNumeric(value, compareValue) > 0,
            "LessThan" => CompareNumeric(value, compareValue) < 0,
            "Contains" => value?.ToString()?.Contains(compareValue ?? "") == true,
            "StartsWith" => value?.ToString()?.StartsWith(compareValue ?? "") == true,
            "EndsWith" => value?.ToString()?.EndsWith(compareValue ?? "") == true,
            _ => false
        };
    }

    private static bool CompareEquals(object? value, string? compareValue)
    {
        if (value == null && compareValue == null) return true;
        if (value == null || compareValue == null) return false;

        // Try numeric comparison
        if (double.TryParse(value.ToString(), out var numValue) &&
            double.TryParse(compareValue, out var numCompare))
        {
            return Math.Abs(numValue - numCompare) < 0.0001;
        }

        // Try boolean comparison
        if (bool.TryParse(value.ToString(), out var boolValue) &&
            bool.TryParse(compareValue, out var boolCompare))
        {
            return boolValue == boolCompare;
        }

        // String comparison
        return string.Equals(value.ToString(), compareValue, StringComparison.OrdinalIgnoreCase);
    }

    private static int CompareNumeric(object? value, string? compareValue)
    {
        if (value == null) return -1;
        if (compareValue == null) return 1;

        if (double.TryParse(value.ToString(), out var numValue) &&
            double.TryParse(compareValue, out var numCompare))
        {
            return numValue.CompareTo(numCompare);
        }

        // Fallback to string comparison
        return string.Compare(value.ToString(), compareValue, StringComparison.OrdinalIgnoreCase);
    }
}
