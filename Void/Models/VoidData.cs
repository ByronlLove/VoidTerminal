using System.Collections.Generic;

namespace VoidTerminal.Models;

public class ShiftRule
{
    public string Target { get; set; } = "";
    public List<int> Shifts { get; set; } = new();
}

public class VoidCharMapping
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class CryptoConfig
{
    public string Mode1Name { get; set; } = "Mode 1";
    public string Mode2Name { get; set; } = "Mode Void";
    public string ResetCharacters { get; set; } = ".";

    public List<ShiftRule> Rules { get; set; } = new();
    public List<VoidCharMapping> VoidMappings { get; set; } = new();

    public CryptoConfig()
    {
        string defaultKeys = "ABCDEFGHIJKLMNOPQRSTUVWXYZ.,'?!";
        string defaultVals = "αβξ∂εΦγℏιψκλμνοπ∅ρστυ∇ωχύζ⊙⌊∧∄†";

        for (int i = 0; i < defaultKeys.Length; i++)
        {
            VoidMappings.Add(new VoidCharMapping { Key = defaultKeys[i].ToString(), Value = defaultVals[i].ToString() });
        }
    }
}

public class VoidData
{
    public HashSet<string> Dictionary { get; set; } = new();
    public HashSet<string> UserAddedWords { get; set; } = new();
    public Dictionary<string, string> Notes { get; set; } = new();
    public string DictProtectionHash { get; set; }
    public CryptoConfig EngineConfig { get; set; } = new CryptoConfig();
}