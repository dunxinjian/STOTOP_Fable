using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

(List<string> vals, int next) ParseValues(string text, int start)
{
    var vals = new List<string>();
    var cur = new StringBuilder();
    bool inStr = false; bool token = false;
    int p = start;
    for (; p < text.Length; p++)
    {
        char c = text[p];
        if (inStr)
        {
            if (c == '\'')
            {
                if (p + 1 < text.Length && text[p + 1] == '\'') { cur.Append('\''); p++; }
                else inStr = false;
            }
            else cur.Append(c);
        }
        else if (c == '\'') { inStr = true; token = true; }
        else if (c == ',') { vals.Add(Norm(cur.ToString(), token)); cur.Clear(); token = false; }
        else if (c == ')') { vals.Add(Norm(cur.ToString(), token)); return (vals, p + 1); }
        else { if (!char.IsWhiteSpace(c) || cur.Length > 0) cur.Append(c); token = true; }
    }
    return (vals, p);
}

string Norm(string raw, bool token)
{
    raw = raw.Trim();
    if (raw == "NULL" || raw.Length == 0) return null;
    if (token && raw.StartsWith("N")) raw = raw.Substring(1).Trim();
    return raw.Replace("\"\"", "\"");
}

var text = File.ReadAllText("src/STOTOP.WebAPI/Data/Seeders/FinanceSeeder.cs");
var marker = "INSERT INTO [FIN阿米巴损益项] (";
int i = 0;
int rowNum = 0;
bool hasMismatch = false;

while ((i = text.IndexOf(marker, i)) >= 0)
{
    int colStart = i + marker.Length;
    int colEnd = text.IndexOf(')', colStart);
    var cols = text.Substring(colStart, colEnd - colStart).Split(',');
    
    int valStart = text.IndexOf("VALUES (", colEnd) + "VALUES (".Length;
    var (vals, next) = ParseValues(text, valStart);
    
    if (cols.Length != vals.Count)
    {
        Console.WriteLine($"Row {rowNum}: MISMATCH cols={cols.Length} vs vals={vals.Count}");
        hasMismatch = true;
        
        // Show which columns would be skipped
        if (cols.Length > vals.Count)
        {
            Console.WriteLine($"  Unmapped columns: {string.Join(", ", cols[vals.Count..].Select(c => c.Trim()))}");
        }
    }
    
    i = next;
    rowNum++;
}

if (!hasMismatch)
{
    Console.WriteLine($"✓ All {rowNum} rows have matching column and value counts");
}
else
{
    Console.WriteLine($"✗ Found {rowNum} total rows, some with mismatches above");
}
