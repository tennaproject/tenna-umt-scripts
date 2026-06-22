#r "System.Windows.Forms"

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

string TennaSelectExportDir()
{
  string sharedExportDir = Environment.GetEnvironmentVariable("TENNA_UMT_EXPORT_DIR");
  if (!string.IsNullOrWhiteSpace(sharedExportDir))
    return sharedExportDir;

  try
  {
    using (FolderBrowserDialog dialog = new FolderBrowserDialog())
    {
      dialog.Description = "Choose export folder";
      dialog.ShowNewFolderButton = true;

      if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        return dialog.SelectedPath;
    }
  }
  catch
  {
  }

  ScriptError("Export cancelled. No folder selected.");
  throw new Exception("Export cancelled. No folder selected.");
}

void TennaExportEnemies()
{
  if (!TennaExportEnemies(TennaSelectExportDir()))
    throw new Exception("DataExportEnemies.csx failed.");
}

bool TennaExportEnemies(string exportDir)
{
  EnsureDataLoaded();
  Directory.CreateDirectory(exportDir);

  UndertaleCode source = TennaGetRequiredScriptCode("scr_monstersetup");
  if (source == null)
  {
    string diagnosticPath = TennaWriteMissingSourceDiagnostic("DataExportEnemies.csx", "scr_monstersetup", exportDir);
    ScriptMessage("Could not find required enemy source script. Diagnostic written to:\n" + diagnosticPath);
    return false;
  }

  TennaWriteEnemyJson(GetDecompiledText(source), exportDir);
  return true;
}

UndertaleCode TennaGetRequiredScriptCode(string sourceName)
{
  UndertaleScript script = Data.Scripts.ByName(sourceName);
  if (script == null || script.Code == null)
    return null;
  return script.Code;
}

string TennaWriteMissingSourceDiagnostic(string scriptName, string sourceName, string exportDir)
{
  string outputPath = Path.Combine(exportDir, scriptName + ".missing-source.txt");

  using (StreamWriter writer = new StreamWriter(outputPath, false))
  {
    writer.WriteLine(scriptName + " could not find its required source script.");
    writer.WriteLine();
    writer.WriteLine("Required source script:");
    writer.WriteLine("- " + sourceName);
  }

  return outputPath;
}

void TennaWriteEnemyJson(string text, string exportDir)
{
  string outputPath = Path.Combine(exportDir, "enemies.json");
  List<TennaEnemy> enemies = TennaParseEnemies(text);

  using (StreamWriter writer = new StreamWriter(outputPath, false))
  {
    writer.WriteLine("{");

    for (int i = 0; i < enemies.Count; i++)
    {
      TennaEnemy enemy = enemies[i];
      if (i > 0)
        writer.WriteLine(",");

      string constantName = TennaConstantName(enemy.Name, "ENEMY", enemy.Id);
      writer.WriteLine("  " + TennaJson(constantName) + ": {");
      writer.WriteLine("    \"id\": " + enemy.Id + ",");
      writer.WriteLine("    \"name\": " + TennaJson(enemy.Name) + ",");
      writer.WriteLine("    \"recruitFlag\": " + (enemy.Id + 600) + ",");
      TennaWriteNullableInt(writer, "hp", enemy.Hp, true);
      TennaWriteNullableInt(writer, "attack", enemy.Attack, true);
      TennaWriteNullableInt(writer, "defense", enemy.Defense, true);
      TennaWriteNullableInt(writer, "exp", enemy.Exp, true);
      TennaWriteNullableInt(writer, "gold", enemy.Gold, true);
      TennaWriteNullableInt(writer, "sparePoint", enemy.SparePoint, true);
      TennaWriteNullableInt(writer, "mercyMax", enemy.MercyMax, false);
      writer.WriteLine("  }");
    }

    writer.WriteLine();
    writer.WriteLine("}");
  }

  ScriptMessage("Exported " + enemies.Count + " enemy entries to:\n" + outputPath);
  if (enemies.Count == 0)
  {
    string rawPath = Path.Combine(Path.GetDirectoryName(outputPath), "DataExportEnemies.csx.source.txt");
    File.WriteAllText(rawPath, text);
    ScriptMessage("No enemy entries parsed from scr_monstersetup. Decompiled source written to:\n" + rawPath);
  }
}

List<TennaEnemy> TennaParseEnemies(string text)
{
  List<TennaEnemy> enemies = new List<TennaEnemy>();
  TennaEnemy current = null;

  using (StringReader reader = new StringReader(text))
  {
    string line;
    while ((line = reader.ReadLine()) != null)
    {
      int id = TennaParseMonsterTypeId(line);
      if (id >= 0)
      {
        if (current != null && current.HasName)
          enemies.Add(current);

        current = new TennaEnemy();
        current.Id = id;
        continue;
      }

      if (current == null)
        continue;

      string name = TennaParseAssignedString(line, "global.monstername");
      if (name != null)
      {
        current.Name = name;
        current.HasName = true;
        continue;
      }

      TennaApplyEnemyNumber(current, line, "global.monstermaxhp", "Hp");
      TennaApplyEnemyNumber(current, line, "global.monsterat", "Attack");
      TennaApplyEnemyNumber(current, line, "global.monsterdf", "Defense");
      TennaApplyEnemyNumber(current, line, "global.monsterexp", "Exp");
      TennaApplyEnemyNumber(current, line, "global.monstergold", "Gold");
      TennaApplyEnemyNumber(current, line, "global.sparepoint", "SparePoint");
      TennaApplyEnemyNumber(current, line, "global.mercymax", "MercyMax");
    }
  }

  if (current != null && current.HasName)
    enemies.Add(current);

  return enemies;
}

int TennaParseMonsterTypeId(string line)
{
  string trimmed = line.Trim();
  string prefix = "if (global.monstertype[myself] ==";
  if (!trimmed.StartsWith(prefix))
    return -1;

  int close = trimmed.IndexOf(")", prefix.Length, StringComparison.Ordinal);
  if (close < 0)
    return -1;

  string number = trimmed.Substring(prefix.Length, close - prefix.Length).Trim();
  int id;
  if (int.TryParse(number, out id))
    return id;
  return -1;
}

void TennaApplyEnemyNumber(TennaEnemy enemy, string line, string assignmentName, string propertyName)
{
  int? value = TennaParseAssignedInt(line, assignmentName);
  if (!value.HasValue)
    return;

  if (propertyName == "Hp")
    enemy.Hp = value;
  else if (propertyName == "Attack")
    enemy.Attack = value;
  else if (propertyName == "Defense")
    enemy.Defense = value;
  else if (propertyName == "Exp")
    enemy.Exp = value;
  else if (propertyName == "Gold")
    enemy.Gold = value;
  else if (propertyName == "SparePoint")
    enemy.SparePoint = value;
  else if (propertyName == "MercyMax")
    enemy.MercyMax = value;
}

string TennaParseAssignedString(string line, string assignmentName)
{
  int equals = line.IndexOf("=", StringComparison.Ordinal);
  if (equals < 0)
    return null;

  string left = line.Substring(0, equals).Trim();
  if (!TennaAssignmentMatches(left, assignmentName))
    return null;

  return TennaParseFirstQuotedString(line, equals + 1);
}

int? TennaParseAssignedInt(string line, string assignmentName)
{
  int equals = line.IndexOf("=", StringComparison.Ordinal);
  if (equals < 0)
    return null;

  string left = line.Substring(0, equals).Trim();
  if (!TennaAssignmentMatches(left, assignmentName))
    return null;

  string right = line.Substring(equals + 1).Trim().TrimEnd(';').Trim();
  int value;
  if (int.TryParse(right, out value))
    return value;
  return null;
}

bool TennaAssignmentMatches(string left, string assignmentName)
{
  if (left == assignmentName)
    return true;
  if (left.StartsWith(assignmentName + "[", StringComparison.Ordinal))
    return true;
  return false;
}

string TennaParseFirstQuotedString(string line, int searchStart)
{
  int firstQuote = line.IndexOf('"', searchStart);
  if (firstQuote < 0)
    return null;
  int secondQuote = line.IndexOf('"', firstQuote + 1);
  if (secondQuote < 0)
    return null;
  return line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
}

void TennaWriteNullableInt(StreamWriter writer, string name, int? value, bool comma)
{
  writer.Write("    \"" + name + "\": ");
  writer.Write(value.HasValue ? value.Value.ToString() : "null");
  if (comma)
    writer.Write(",");
  writer.WriteLine();
}

string TennaConstantName(string rawName, string constantPrefix, int id)
{
  string output = "";
  bool lastUnderscore = false;
  for (int i = 0; i < rawName.Length; i++)
  {
    char c = rawName[i];
    bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
    if (ok)
    {
      output += char.ToUpperInvariant(c);
      lastUnderscore = false;
    }
    else if (!lastUnderscore && output.Length > 0)
    {
      output += "_";
      lastUnderscore = true;
    }
  }
  output = output.Trim('_');
  if (output.Length == 0)
    output = constantPrefix + "_" + id;
  if (output[0] >= '0' && output[0] <= '9')
    output = constantPrefix + "_" + output;
  return output;
}

string TennaJson(string value)
{
  if (value == null)
    return "null";
  string output = "\"";
  for (int i = 0; i < value.Length; i++)
  {
    char c = value[i];
    if (c == '\\') output += "\\\\";
    else if (c == '"') output += "\\\"";
    else if (c == '\n') output += "\\n";
    else if (c == '\r') output += "\\r";
    else if (c == '\t') output += "\\t";
    else output += c;
  }
  return output + "\"";
}

class TennaEnemy
{
  public int Id;
  public string Name = "";
  public bool HasName;
  public int? Hp;
  public int? Attack;
  public int? Defense;
  public int? Exp;
  public int? Gold;
  public int? SparePoint;
  public int? MercyMax;
}

TennaExportEnemies();
