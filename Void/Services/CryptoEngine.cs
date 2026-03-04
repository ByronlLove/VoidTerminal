using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VoidTerminal.Models;

namespace VoidTerminal.Services;

public class DecryptSegment
{
    public string Text { get; set; }
    public bool IsAmbiguous { get; set; }
    public List<string> Possibilities { get; set; }
}

public static class CryptoEngine
{
    private static readonly string Voyelles = "AEIOUYaeiouy";

    // --- LE NETTOYEUR D'ACCENTS UNIVERSEL ---
    private static char RemoveAccent(char c)
    {
        if (c == 'œ') return 'o';
        if (c == 'Œ') return 'O';
        if (c == 'æ') return 'a';
        if (c == 'Æ') return 'A';

        string normalized = c.ToString().Normalize(NormalizationForm.FormD);
        foreach (char ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                return ch;
            }
        }
        return c;
    }

    private static bool IsVowel(char c) => Voyelles.Contains(char.ToUpper(c));

    private static bool MatchesTarget(char c, string target)
    {
        char upperC = char.ToUpper(c);
        bool isVoyelle = IsVowel(c);
        if (target.StartsWith("Lettre:") && target.Length > 7) return target[7] == upperC;
        if (target == "Voyelles") return isVoyelle;
        if (target == "Consonnes") return !isVoyelle;
        return false;
    }

    public static string To125(string text, CryptoConfig config)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string shifted = ShiftChar(text, config);
        var temp = new StringBuilder(shifted);

        if (config != null && config.VoidMappings != null)
        {
            foreach (var m in config.VoidMappings.Where(x => !string.IsNullOrEmpty(x.Key) && !char.IsLetter(x.Key[0])))
            {
                if (!string.IsNullOrEmpty(m.Value)) temp.Replace(m.Key, m.Value);
            }
        }
        return temp.ToString();
    }

    public static string ToVoid(string text, CryptoConfig config)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string shifted = ShiftChar(text, config).ToUpper();
        var temp = new StringBuilder(shifted);
        temp.Replace(" ", "(_)");

        if (config != null && config.VoidMappings != null)
        {
            foreach (var m in config.VoidMappings)
            {
                if (!string.IsNullOrEmpty(m.Key) && !string.IsNullOrEmpty(m.Value))
                    temp.Replace(m.Key, m.Value);
            }
        }
        return temp.ToString();
    }

    // --- ENCRYPTAGE : MACHINE À ÉTATS PAR BLOCS ET VIRGULES ---
    private static string ShiftChar(string message, CryptoConfig config)
    {
        if (config == null || config.Rules == null || config.Rules.Count == 0) return message;

        // On utilise un index global pour les blocs, et un tableau pour les virgules de chaque règle
        int globalRuleIndex = 0;
        int[] ruleCursors = new int[config.Rules.Count];
        var res = new StringBuilder();

        foreach (char originalChar in message)
        {
            char c = RemoveAccent(originalChar);

            if (!char.IsLetter(c))
            {
                if (!string.IsNullOrEmpty(config.ResetCharacters) && config.ResetCharacters.Contains(c))
                {
                    globalRuleIndex = 0;
                    Array.Clear(ruleCursors, 0, ruleCursors.Length);
                }
                res.Append(c);
                continue;
            }

            bool matchFound = false;
            int attempts = 0;

            // On cherche la règle active pour ce bloc
            while (attempts < config.Rules.Count)
            {
                var rule = config.Rules[globalRuleIndex];
                if (MatchesTarget(c, rule.Target))
                {
                    matchFound = true;
                    break;
                }
                // Si la cible ne correspond pas, le bloc est cassé, on passe à la règle suivante
                globalRuleIndex = (globalRuleIndex + 1) % config.Rules.Count;
                attempts++;
            }

            if (matchFound)
            {
                var activeRule = config.Rules[globalRuleIndex];
                int shift = activeRule.Shifts[ruleCursors[globalRuleIndex]];

                int normalizedShift = shift % 26;
                if (normalizedShift < 0) normalizedShift += 26;

                char dep = char.IsUpper(c) ? 'A' : 'a';
                res.Append((char)((c - dep + normalizedShift) % 26 + dep));

                // On fait avancer la virgule (le curseur interne) de CETTE règle
                ruleCursors[globalRuleIndex] = (ruleCursors[globalRuleIndex] + 1) % activeRule.Shifts.Count;
            }
            else
            {
                res.Append(c);
            }
        }
        return res.ToString();
    }

    // --- DÉCRYPTAGE INTELLIGENT PRÉDICTIF ---
    private class StateResult
    {
        public string Text { get; set; }
        public int GlobalRuleIndex { get; set; }
        public int[] RuleCursors { get; set; }
    }

    public static List<DecryptSegment> FromVoidSmart(string text, HashSet<string> dictionary, CryptoConfig config)
    {
        var segments = new List<DecryptSegment>();
        if (string.IsNullOrEmpty(text)) return segments;

        string temp = text.Replace("(_)", " ").Replace("ς", "S").Replace("σ", "S");

        if (config != null && config.VoidMappings != null)
        {
            var sortedMappings = config.VoidMappings.Where(m => !string.IsNullOrEmpty(m.Key) && !string.IsNullOrEmpty(m.Value)).OrderByDescending(m => m.Value.Length).ToList();
            foreach (var m in sortedMappings) temp = temp.Replace(m.Value, m.Key);
        }

        var tokens = Regex.Matches(temp, @"[\w']+|[^\w\s]|\s+");

        int globalRuleIndex = 0;
        int[] globalRuleCursors = config?.Rules != null ? new int[config.Rules.Count] : new int[0];

        foreach (Match match in tokens)
        {
            string token = match.Value;

            if (!char.IsLetter(RemoveAccent(token[0])))
            {
                if (config?.Rules != null && config.Rules.Count > 0 && !string.IsNullOrEmpty(config.ResetCharacters))
                {
                    foreach (char c in token)
                    {
                        if (config.ResetCharacters.Contains(c))
                        {
                            globalRuleIndex = 0;
                            Array.Clear(globalRuleCursors, 0, globalRuleCursors.Length);
                        }
                    }
                }
                segments.Add(new DecryptSegment { Text = token, IsAmbiguous = false });
                continue;
            }

            if (config?.Rules == null || config.Rules.Count == 0)
            {
                segments.Add(new DecryptSegment { Text = MettreEnFormePhrase(token), IsAmbiguous = false });
                continue;
            }

            var poss = GetStatefulPossibilities(token, config, globalRuleIndex, globalRuleCursors);
            var valid = poss.Where(p => dictionary.Contains(p.Text.ToLower())).ToList();

            if (valid.Count > 0)
            {
                segments.Add(new DecryptSegment { Text = MettreEnFormePhrase(valid[0].Text), IsAmbiguous = false });
                globalRuleIndex = valid[0].GlobalRuleIndex;
                globalRuleCursors = valid[0].RuleCursors;
            }
            else
            {
                var defaultPoss = poss.Count > 0 ? poss[0] : new StateResult { Text = token, GlobalRuleIndex = globalRuleIndex, RuleCursors = (int[])globalRuleCursors.Clone() };
                var allTexts = poss.Select(p => p.Text).Distinct().ToList();
                segments.Add(new DecryptSegment { Text = $"[{defaultPoss.Text} ?]", IsAmbiguous = true, Possibilities = allTexts });

                globalRuleIndex = defaultPoss.GlobalRuleIndex;
                globalRuleCursors = defaultPoss.RuleCursors;
            }
        }
        return segments;
    }

    private static List<StateResult> GetStatefulPossibilities(string token, CryptoConfig config, int startGlobalIndex, int[] startCursors)
    {
        var results = new List<StateResult> { new StateResult {
            Text = "",
            GlobalRuleIndex = startGlobalIndex,
            RuleCursors = (int[])startCursors.Clone()
        }};

        foreach (char originalChar in token)
        {
            char c = RemoveAccent(originalChar);
            var next = new List<StateResult>();
            char dep = char.IsUpper(c) ? 'A' : 'a';

            foreach (var state in results)
            {
                bool matchedAny = false;

                // L'algorithme teste récursivement toutes les règles possibles pour retrouver l'origine de la lettre
                for (int attempts = 0; attempts < config.Rules.Count; attempts++)
                {
                    int testRuleIndex = (state.GlobalRuleIndex + attempts) % config.Rules.Count;
                    var rule = config.Rules[testRuleIndex];

                    int cursor = state.RuleCursors[testRuleIndex];
                    int shift = rule.Shifts[cursor];

                    int normalizedShift = shift % 26;
                    if (normalizedShift < 0) normalizedShift += 26;

                    // On applique le décalage INVERSE
                    char unshifted = (char)((c - dep - normalizedShift + 26) % 26 + dep);

                    // Si la lettre inversée correspond BIEN à la cible de la règle testée, c'est une possibilité valide !
                    if (MatchesTarget(unshifted, rule.Target))
                    {
                        var newCursors = (int[])state.RuleCursors.Clone();
                        newCursors[testRuleIndex] = (newCursors[testRuleIndex] + 1) % rule.Shifts.Count;

                        next.Add(new StateResult
                        {
                            Text = state.Text + unshifted,
                            GlobalRuleIndex = testRuleIndex, // Le bloc a peut-être avancé
                            RuleCursors = newCursors
                        });
                        matchedAny = true;
                    }
                }

                if (!matchedAny)
                {
                    next.Add(new StateResult
                    {
                        Text = state.Text + c,
                        GlobalRuleIndex = state.GlobalRuleIndex,
                        RuleCursors = (int[])state.RuleCursors.Clone()
                    });
                }
            }
            results = next;
            if (results.Count == 0) break;
        }
        return results;
    }

    private static string MettreEnFormePhrase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return char.ToUpper(text[0]) + text.Substring(1).ToLower();
    }
}