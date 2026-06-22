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

void TennaExportRooms()
{
  if (!TennaExportRooms(TennaSelectExportDir()))
    throw new Exception("ExportRooms.csx failed.");
}

bool TennaExportRooms(string exportDir)
{
  EnsureDataLoaded();
  Directory.CreateDirectory(exportDir);

  UndertaleCode roomListSource = TennaGetRequiredScriptCode("scr_get_room_by_id");
  if (roomListSource == null)
  {
    string diagnosticPath = TennaWriteMissingSourceDiagnostic("ExportRooms.csx", "scr_get_room_by_id", exportDir);
    ScriptMessage("Could not find required room list source script. Diagnostic written to:\n" + diagnosticPath);
    return false;
  }

  Dictionary<string, int> roomListEntries = TennaParseRoomListEntries(GetDecompiledText(roomListSource));
  Dictionary<int, string> saveNames = TennaReadRoomSaveNames();
  TennaWriteRoomListJson(roomListEntries, saveNames, exportDir);
  return true;
}

UndertaleCode TennaGetRequiredScriptCode(string sourceName)
{
  UndertaleScript script = Data.Scripts.ByName(sourceName);
  if (script == null || script.Code == null)
    return null;
  return script.Code;
}

UndertaleCode TennaGetOptionalScriptCode(string sourceName)
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

Dictionary<string, int> TennaParseRoomListEntries(string text)
{
  Dictionary<string, int> entries = new Dictionary<string, int>();

  int searchStart = 0;
  while (true)
  {
    int entryStart = text.IndexOf("new scr_room(", searchStart, StringComparison.Ordinal);
    if (entryStart < 0)
      break;

    int argsStart = entryStart + "new scr_room(".Length;
    int argsEnd = text.IndexOf(")", argsStart, StringComparison.Ordinal);
    if (argsEnd < 0)
      break;

    string args = text.Substring(argsStart, argsEnd - argsStart);
    string roomName;
    int roomId;
    if (TennaParseRoomEntry(args, out roomName, out roomId))
      entries[roomName] = roomId;

    searchStart = argsEnd + 1;
  }

  return entries;
}

Dictionary<int, string> TennaReadRoomSaveNames()
{
  UndertaleCode source = TennaGetOptionalScriptCode("scr_roomname");
  if (source == null)
    return new Dictionary<int, string>();

  return TennaParseRoomSaveNames(GetDecompiledText(source));
}

Dictionary<int, string> TennaParseRoomSaveNames(string text)
{
  Dictionary<int, string> names = new Dictionary<int, string>();

  using (StringReader reader = new StringReader(text))
  {
    string line;
    int pendingId = -1;
    while ((line = reader.ReadLine()) != null)
    {
      int id = TennaParseRoomNameId(line);
      if (id >= 0)
        pendingId = id;

      string name = TennaParseStringSetLocName(line);
      if (pendingId >= 0 && name != null)
      {
        names[pendingId] = name;
        pendingId = -1;
      }
    }
  }

  return names;
}

int TennaParseRoomNameId(string line)
{
  string trimmed = line.Trim();
  if (!trimmed.StartsWith("if (arg0 =="))
    return -1;

  int marker = trimmed.IndexOf("==", StringComparison.Ordinal);
  int close = trimmed.IndexOf(")", marker + 2, StringComparison.Ordinal);
  if (marker < 0 || close < 0)
    return -1;

  string number = trimmed.Substring(marker + 2, close - marker - 2).Trim();
  int id;
  if (int.TryParse(number, out id))
    return id;
  return -1;
}

string TennaParseStringSetLocName(string line)
{
  int marker = line.IndexOf("stringsetloc(", StringComparison.OrdinalIgnoreCase);
  if (marker < 0)
    return null;

  int firstQuote = line.IndexOf('"', marker);
  if (firstQuote < 0)
    return null;
  int secondQuote = line.IndexOf('"', firstQuote + 1);
  if (secondQuote < 0)
    return null;

  return line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
}

void TennaWriteRoomListJson(Dictionary<string, int> roomListEntries, Dictionary<int, string> saveNames, string exportDir)
{
  string outputPath = Path.Combine(exportDir, "rooms.json");
  int count = 0;

  using (StreamWriter writer = new StreamWriter(outputPath, false))
  {
    writer.WriteLine("{");

    foreach (var room in Data.Rooms)
    {
      string roomName = "";
      try { roomName = room.Name.Content; } catch { continue; }
      if (string.IsNullOrWhiteSpace(roomName))
        continue;

      if (count > 0)
        writer.WriteLine(",");

      bool inRoomList = roomListEntries.ContainsKey(roomName);
      bool dogcheck = !inRoomList;
      int roomIndex = Data.Rooms.IndexOf(room);

      if (inRoomList)
      {
        int roomId = roomListEntries[roomName];
        int inferredChapter = roomId >= 10000 ? roomId / 10000 : 0;
        int shortRoomId = roomId >= 10000 ? roomId % 10000 : roomId;
        string saveName = null;
        if (saveNames.ContainsKey(roomId))
          saveName = saveNames[roomId];
        bool hasSaveName = !string.IsNullOrWhiteSpace(saveName) && saveName != "---" && saveName != "Dark World?";
        string constantName = TennaRoomConstantName(roomName, "ROOM", roomId);

        writer.WriteLine("  " + TennaJson(constantName) + ": {");
        writer.WriteLine("    \"id\": " + roomId + ",");
        writer.WriteLine("    \"shortId\": " + shortRoomId + ",");
        writer.WriteLine("    \"name\": " + TennaJson(roomName) + ",");
        writer.WriteLine("    \"roomIndexName\": " + TennaJson(roomName) + ",");
        writer.WriteLine("    \"saveName\": " + TennaJson(saveName) + ",");
        writer.WriteLine("    \"hasSaveName\": " + (hasSaveName ? "true" : "false") + ",");
        writer.WriteLine("    \"inferredChapter\": " + (inferredChapter == 0 ? "null" : inferredChapter.ToString()) + ",");
        writer.WriteLine("    \"dogcheck\": " + (dogcheck ? "true" : "false"));
      }
      else
      {
        string constantName = TennaRoomConstantName(roomName, "ROOM", roomIndex);

        writer.WriteLine("  " + TennaJson(constantName) + ": {");
        writer.WriteLine("    \"id\": " + roomIndex + ",");
        writer.WriteLine("    \"name\": " + TennaJson(roomName) + ",");
        writer.WriteLine("    \"dogcheck\": " + (dogcheck ? "true" : "false"));
      }

      writer.Write("  }");
      count++;
    }

    writer.WriteLine();
    writer.WriteLine("}");
  }

  ScriptMessage("Exported " + count + " room entries to:\n" + outputPath);
}

bool TennaParseRoomEntry(string args, out string roomName, out int roomId)
{
  roomName = "";
  roomId = -1;

  int comma = args.IndexOf(",", StringComparison.Ordinal);
  if (comma < 0)
    return false;

  roomName = args.Substring(0, comma).Trim();
  string idText = args.Substring(comma + 1).Trim();
  return roomName.Length > 0 && int.TryParse(idText, out roomId);
}

string TennaRoomConstantName(string rawName, string constantPrefix, int id)
{
  string value = rawName;
  if (value.StartsWith("room_"))
    value = value.Substring(5);
  return TennaConstantName(value, constantPrefix, id);
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

TennaExportRooms();
