#r "System.Windows.Forms"

using System;
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

void TennaExportLightWorldItems()
{
  if (!TennaExportLightWorldItems(TennaSelectExportDir()))
    throw new Exception("DataExportLightWorldItems.csx failed.");
}

bool TennaExportLightWorldItems(string exportDir)
{
  return TennaExportExactNameTable("light-world item", "DataExportLightWorldItems.csx", "light-world-items.json", "LIGHT_ITEM", "scr_litemname", "equality", "global.litemname", exportDir);
}

bool TennaExportExactNameTable(string label, string scriptName, string fileName, string constantPrefix, string sourceName, string idMode, string assignmentName, string exportDir)
{
  EnsureDataLoaded();
  Directory.CreateDirectory(exportDir);
  UndertaleCode source = TennaGetRequiredScriptCode(sourceName);

  if (source == null)
  {
    string diagnosticPath = TennaWriteMissingSourceDiagnostic(scriptName, sourceName, exportDir);
    ScriptMessage("Could not find required " + label + " source script. Diagnostic written to:\n" + diagnosticPath);
    return false;
  }

  TennaWriteNameTableJson(scriptName, fileName, constantPrefix, sourceName, idMode, assignmentName, GetDecompiledText(source), exportDir);
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

void TennaWriteNameTableJson(string scriptName, string fileName, string constantPrefix, string sourceName, string idMode, string assignmentName, string text, string exportDir)
{
  string outputPath = Path.Combine(exportDir, fileName);
  int count = 0;

  using (StreamWriter writer = new StreamWriter(outputPath, false))
  {
    writer.WriteLine("{");

    using (StringReader reader = new StringReader(text))
    {
      string line;
      int pendingId = -1;
      while ((line = reader.ReadLine()) != null)
      {
        int id = -1;
        if (idMode == "case")
          id = TennaParseCaseId(line);
        else if (idMode == "equality")
          id = TennaParseEqualityId(line);

        if (id >= 0)
          pendingId = id;

        string name = TennaParseAssignedName(line, assignmentName);
        if (pendingId >= 0 && name != null)
        {
          if (count > 0)
            writer.WriteLine(",");
          string constantName = TennaConstantName(name, constantPrefix, pendingId);
          writer.Write("  " + TennaJson(constantName) + ": { \"id\": " + pendingId + ", \"name\": " + TennaJson(name) + " }");
          count++;
          pendingId = -1;
        }
      }
    }

    writer.WriteLine();
    writer.WriteLine("}");
  }

  ScriptMessage("Exported " + count + " entries to:\n" + outputPath);
  if (count == 0)
  {
    string rawPath = Path.Combine(Path.GetDirectoryName(outputPath), scriptName + ".source.txt");
    File.WriteAllText(rawPath, text);
    ScriptMessage("No entries parsed from " + sourceName + ". Decompiled source written to:\n" + rawPath);
  }
}

int TennaParseCaseId(string line)
{
  string trimmed = line.Trim();
  if (!trimmed.StartsWith("case "))
    return -1;
  int colon = trimmed.IndexOf(":");
  if (colon < 0)
    return -1;
  string number = trimmed.Substring(5, colon - 5).Trim();
  int id;
  if (int.TryParse(number, out id))
    return id;
  return -1;
}

int TennaParseEqualityId(string line)
{
  string trimmed = line.Trim();
  if (!trimmed.StartsWith("if ("))
    return -1;

  int marker = trimmed.IndexOf("==", StringComparison.Ordinal);
  if (marker < 0)
    return -1;
  int close = trimmed.IndexOf(")", marker + 2, StringComparison.Ordinal);
  if (close < 0)
    return -1;

  string number = trimmed.Substring(marker + 2, close - marker - 2).Trim();
  int id;
  if (int.TryParse(number, out id))
    return id;
  return -1;
}

string TennaParseAssignedName(string line, string assignmentName)
{
  int equals = line.IndexOf("=", StringComparison.Ordinal);
  if (equals < 0)
    return null;

  string left = line.Substring(0, equals).Trim();
  if (!TennaAssignmentMatches(left, assignmentName))
    return null;

  return TennaParseFirstQuotedString(line, equals + 1);
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

TennaExportLightWorldItems();
