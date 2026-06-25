EnsureDataLoaded();

if (Data.Code.ByName("gml_Object_obj_time_Create_0") is not UndertaleCode createCode)
{
  ScriptError("Failed to find obj_time Create event.");
  return;
}
if (Data.Code.ByName("gml_Object_obj_time_Step_1") is not UndertaleCode stepCode)
{
  ScriptError("Failed to find obj_time Step_1 event.");
  return;
}
if (Data.Code.ByName("gml_Object_obj_time_Draw_64") is not UndertaleCode drawCode)
{
  ScriptError("Failed to find obj_time Draw_64 event.");
  return;
}

string checkCreate = GetDecompiledText(createCode);

if (!checkCreate.Contains("_tenna_core_enabled"))
{
  ScriptError("Tenna Core is required!\n\nPlease install GameCore.csx first.");
  return;
}

bool flagWatcherAlreadyInstalled = checkCreate.Contains("_tenna_fw_enabled");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
// TENNA_FLAG_WATCHER_CREATE_BEGIN
_tenna_fw_max_log = 30;
_tenna_fw_enabled = true;
_tenna_fw_visible = true;
directory_create(""tenna"");
directory_create(""tenna/flag-logs"");
global._tenna_fw_export_filename = ""tenna/flag-logs/flags-"" + global._tenna_core_ts + "".jsonl"";
global._tenna_loading_save = false;
global._tenna_fw_frame_writes = 0;

var _tenna_fw_room = -1;
if (variable_global_exists(""currentroom""))
    _tenna_fw_room = global.currentroom;
var _tenna_fw_plot = -1;
if (variable_global_exists(""plot""))
    _tenna_fw_plot = global.plot;
var _tenna_fw_chapter = -1;
if (variable_global_exists(""chapter""))
    _tenna_fw_chapter = global.chapter;

_tenna_fw_flag_count = 0;
_tenna_fw_shadow = array_create(0);
if (variable_global_exists(""flag"") && is_array(global.flag))
{
    _tenna_fw_flag_count = array_length(global.flag);
    _tenna_fw_shadow = array_create(_tenna_fw_flag_count);
    
    var _tenna_fw_file = file_text_open_append(global._tenna_fw_export_filename);
    for (var _tenna_fw_i = 0; _tenna_fw_i < _tenna_fw_flag_count; _tenna_fw_i++)
    {
        _tenna_fw_shadow[_tenna_fw_i] = global.flag[_tenna_fw_i];

        if (_tenna_fw_i == 6 || _tenna_fw_i == 20 || _tenna_fw_i == 21 || _tenna_fw_i == 33)
            continue;

        scr_tenna_fw_write_row(_tenna_fw_file, ""baseline"", _tenna_fw_i, 0, global.flag[_tenna_fw_i], 0, _tenna_fw_chapter, _tenna_fw_room, _tenna_fw_plot);
    }
    file_text_close(_tenna_fw_file);
    scr_tenna_log(""FlagWatcher"", ""baseline written for watched flags 0.."" + string(_tenna_fw_flag_count - 1));
}

for (var _tenna_fw_i = 0; _tenna_fw_i < _tenna_fw_max_log; _tenna_fw_i++)
{
    _tenna_fw_log[_tenna_fw_i] = """";
    _tenna_fw_alpha[_tenna_fw_i] = 0;
}
// TENNA_FLAG_WATCHER_CREATE_END
";

string stepCheck = @"
// TENNA_FLAG_WATCHER_STEP_BEGIN
global._tenna_fw_frame_writes = 0;
if (global._tenna_loading_save)
    global._tenna_loading_save = false;

if (keyboard_check_pressed(ord(""2"")) && keyboard_check(vk_alt))
    _tenna_fw_visible = !_tenna_fw_visible;

if (_tenna_fw_enabled)
{
    for (var _tenna_fw_i = 0; _tenna_fw_i < _tenna_fw_max_log; _tenna_fw_i++)
    {
        if (_tenna_fw_alpha[_tenna_fw_i] > 0)
            _tenna_fw_alpha[_tenna_fw_i] -= 0.003;
    }
}
// TENNA_FLAG_WATCHER_STEP_END
";

string drawDisplay = @"
// TENNA_FLAG_WATCHER_DRAW_BEGIN
if (_tenna_fw_visible)
{
    draw_set_font(fnt_main);
    draw_set_halign(fa_right);
    var _tenna_fw_yoff = 8;
    for (var _tenna_fw_i = 0; _tenna_fw_i < _tenna_fw_max_log; _tenna_fw_i++)
    {
        if (_tenna_fw_log[_tenna_fw_i] != """" && _tenna_fw_alpha[_tenna_fw_i] > 0)
        {
            draw_set_alpha(_tenna_fw_alpha[_tenna_fw_i]);
            draw_set_color(c_black);
            draw_text(633, _tenna_fw_yoff + 1, _tenna_fw_log[_tenna_fw_i]);
            draw_set_color(c_yellow);
            draw_text(632, _tenna_fw_yoff, _tenna_fw_log[_tenna_fw_i]);
            _tenna_fw_yoff += 14;
        }
    }
    draw_set_alpha(1);
    draw_set_halign(fa_left);
    draw_set_color(c_white);
}
// TENNA_FLAG_WATCHER_DRAW_END
";

var writeRowFunctionName = "scr_tenna_fw_write_row";
UndertaleCode writeRowCode;
if (Data.Scripts.ByName(writeRowFunctionName)?.Code is UndertaleCode existingWriteRowCode)
{
  writeRowCode = existingWriteRowCode;
}
else
{
  writeRowCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + writeRowFunctionName) };
  Data.Code.Add(writeRowCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(writeRowFunctionName), Code = writeRowCode };
  Data.Scripts.Add(scriptEntry);
}

string writeRowBody = @"
var _file = argument0;
var _event = argument1;
var _flag_id = argument2;
var _old_value = argument3;
var _new_value = argument4;
var _elapsed = argument5;
var _chapter = argument6;
var _room = argument7;
var _plot = argument8;
var _row = ds_map_create();

ds_map_add(_row, ""event"", _event);
ds_map_add(_row, ""elapsedSeconds"", _elapsed);
ds_map_add(_row, ""flagId"", _flag_id);
if (_event == ""baseline"")
{
    ds_map_add(_row, ""value"", _new_value);
}
else
{
    ds_map_add(_row, ""oldValue"", _old_value);
    ds_map_add(_row, ""newValue"", _new_value);
}

ds_map_add(_row, ""chapter"", _chapter);
ds_map_add(_row, ""room"", _room);
ds_map_add(_row, ""plot"", _plot);
file_text_write_string(_file, json_encode(_row));
file_text_writeln(_file);
ds_map_destroy(_row);
";
importGroup.QueueReplace(writeRowCode, writeRowBody);

var flagSetFunctionName = "scr_tenna_flag_set";
UndertaleCode flagSetCode;
if (Data.Scripts.ByName(flagSetFunctionName)?.Code is UndertaleCode existingFlagSetCode)
{
  flagSetCode = existingFlagSetCode;
}
else
{
  flagSetCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + flagSetFunctionName) };
  Data.Code.Add(flagSetCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(flagSetFunctionName), Code = flagSetCode };
  Data.Scripts.Add(scriptEntry);
}

string flagSetBody = @"
var _index = argument0;
var _value = argument1;

var _len = 0;
if (variable_global_exists(""flag"") && is_array(global.flag))
    _len = array_length(global.flag);

var _old = 0;
if (_index < _len)
    _old = global.flag[_index];

global.flag[_index] = _value;

if (!variable_global_exists(""_tenna_core_enabled"") || !global._tenna_core_enabled)
    return 0;

if (variable_global_exists(""_tenna_loading_save"") && global._tenna_loading_save)
    return 0;

if (variable_global_exists(""_tenna_fw_frame_writes""))
{
    global._tenna_fw_frame_writes++;
    if (global._tenna_fw_frame_writes > 50)
    {
        global._tenna_loading_save = true;
        return 0;
    }
}

if (_index == 6 || _index == 20 || _index == 21 || _index == 33)
    return 0;

if (_old == _value)
{
    if (_index < 900 || _index > 911)
        return 0;
}

var _tenna_fw_room = -1;
if (variable_global_exists(""currentroom""))
    _tenna_fw_room = global.currentroom;
var _tenna_fw_plot = -1;
if (variable_global_exists(""plot""))
    _tenna_fw_plot = global.plot;
var _tenna_fw_chapter = -1;
if (variable_global_exists(""chapter""))
    _tenna_fw_chapter = global.chapter;

var _tenna_fw_elapsed = (current_time - global._tenna_core_start_time) / 1000;
var _tenna_fw_file = file_text_open_append(global._tenna_fw_export_filename);

scr_tenna_fw_write_row(_tenna_fw_file, ""change"", _index, _old, _value, _tenna_fw_elapsed, _tenna_fw_chapter, _tenna_fw_room, _tenna_fw_plot);
file_text_close(_tenna_fw_file);

scr_tenna_log(""FlagWatcher"", ""["" + string(_index) + ""]: "" + string(_old) + "" -> "" + string(_value) + "" room="" + string(_tenna_fw_room) + "" plot="" + string(_tenna_fw_plot));

if (instance_exists(obj_time))
{
    with (obj_time)
    {
        for (var _tenna_fw_j = _tenna_fw_max_log - 1; _tenna_fw_j > 0; _tenna_fw_j--)
        {
            _tenna_fw_log[_tenna_fw_j] = _tenna_fw_log[_tenna_fw_j - 1];
            _tenna_fw_alpha[_tenna_fw_j] = _tenna_fw_alpha[_tenna_fw_j - 1];
        }
        _tenna_fw_log[0] = ""Flag["" + string(_index) + ""]: "" + string(_old) + "" -> "" + string(_value);
        _tenna_fw_alpha[0] = 1;
    }
}
return 0;
";
importGroup.QueueReplace(flagSetCode, flagSetBody);

try
{
  string currentStepText = GetDecompiledText(stepCode);
  string currentDrawText = GetDecompiledText(drawCode);
  string currentCreateText = GetDecompiledText(createCode);

  string cleanCreate = TennaCleanAllBlocks(currentCreateText, "_tenna_fw_max_log = 30;", "_tenna_fw_alpha[_tenna_fw_i] = 0;");
  importGroup.QueueReplace(createCode, cleanCreate + createInit);

  string cleanStep = TennaCleanAllBlocks(currentStepText, "keyboard_check_pressed(ord(\"2\"))", "_tenna_fw_alpha[_tenna_fw_i] -= 0.003;");
  importGroup.QueueReplace(stepCode, stepCheck + cleanStep);

  string cleanDraw = TennaCleanAllBraceBlocks(currentDrawText, "_tenna_fw_visible");
  importGroup.QueueReplace(drawCode, cleanDraw + drawDisplay);

  importGroup.Import();
  


  // Hook global.flag assignments
  int hookedCount = 0;
  int failedCount = 0;
  List<string> errorLog = new List<string>();

  List<UndertaleCode> codeSnapshot = new List<UndertaleCode>();
  foreach (var code in Data.Code)
    codeSnapshot.Add(code);

  foreach (var code in codeSnapshot)
  {
    if (code.Name.Content.StartsWith("gml_Script_scr_tenna_"))
      continue;
    if (code.Name.Content == "gml_Object_obj_time_Create_0" || code.Name.Content == "gml_Object_obj_time_Step_1" || code.Name.Content == "gml_Object_obj_time_Draw_64")
      continue;

    if (!ReferencesFlag(code))
      continue;

    string originalText = GetDecompiledText(code);
    if (originalText.Contains("global.flag"))
    {
      string modifiedText = HookFlagAssignments(originalText);
      if (modifiedText != originalText)
      {
        try
        {
          UndertaleModLib.Compiler.CodeImportGroup localGroup = new(Data) { ThrowOnNoOpFindReplace = false };
          localGroup.QueueReplace(code, modifiedText);
          localGroup.Import();
          hookedCount++;
        }
        catch (Exception ex)
        {
          errorLog.Add("Script: " + code.Name.Content + "\nError: " + ex.Message + "\n----------------------------------------\n");
          try
          {
            UndertaleModLib.Compiler.CodeImportGroup restoreGroup = new(Data) { ThrowOnNoOpFindReplace = false };
            restoreGroup.QueueReplace(code, originalText);
            restoreGroup.Import();
          }
          catch (Exception) { }
          failedCount++;
        }
      }
    }
  }

  if (errorLog.Count > 0)
  {
    System.IO.Directory.CreateDirectory("tenna");
    System.IO.File.WriteAllLines("tenna/flag-watcher-errors.txt", errorLog.ToArray());
  }

  if (Environment.GetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES") != "1")
  {
    string msg = "Flag Watcher " + (flagWatcherAlreadyInstalled ? "updated" : "installed") + "!\n\nAlt+2 to toggle display.\nSuccessfully hooked: " + hookedCount + " scripts.";
    if (failedCount > 0)
      msg += "\n\nWarning: " + failedCount + " scripts failed compilation and were skipped.\nDetails written to tenna/flag-watcher-errors.txt";
    ScriptMessage(msg);
  }
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
}

string HookFlagAssignments(string codeText)
{
  int index = 0;
  while (true)
  {
    index = FindNextCodeGlobalFlag(codeText, index);
    if (index < 0)
      break;

    int openBracket = codeText.IndexOf('[', index + "global.flag".Length);
    if (openBracket < 0 || openBracket - (index + "global.flag".Length) > 5)
    {
      index += 11;
      continue;
    }

    int bracketCount = 1;
    int closeBracket = openBracket + 1;
    while (closeBracket < codeText.Length && bracketCount > 0)
    {
      char c = codeText[closeBracket];
      if (IsStringStart(codeText, closeBracket))
      {
        closeBracket = SkipStringLiteral(codeText, closeBracket);
        continue;
      }
      if (c == '[')
        bracketCount++;
      else if (c == ']')
        bracketCount--;

      if (bracketCount > 0)
        closeBracket++;
    }

    if (bracketCount > 0)
    {
      index += 11;
      continue;
    }

    string indexExpr = codeText.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();

    int scan = closeBracket + 1;
    while (scan < codeText.Length && char.IsWhiteSpace(codeText[scan]))
      scan++;

    if (scan >= codeText.Length)
      break;

    string op = "";
    if (scan + 1 < codeText.Length && (codeText[scan] == '+' || codeText[scan] == '-' || codeText[scan] == '*' || codeText[scan] == '/') && codeText[scan + 1] == '=')
    {
      op = codeText.Substring(scan, 2);
      scan += 2;
    }
    else if (codeText[scan] == '=')
    {
      if (scan + 1 < codeText.Length && codeText[scan + 1] == '=')
      {
        index = closeBracket + 1;
        continue;
      }
      op = "=";
      scan += 1;
    }
    else if (scan + 1 < codeText.Length && (codeText[scan] == '+' && codeText[scan + 1] == '+'))
    {
      op = "++";
      scan += 2;
    }
    else if (scan + 1 < codeText.Length && (codeText[scan] == '-' && codeText[scan + 1] == '-'))
    {
      op = "--";
      scan += 2;
    }

    if (op == "")
    {
      index = closeBracket + 1;
      continue;
    }

    int endAssign = scan;
    int parenCount = 0;
    int curlyCount = 0;
    while (endAssign < codeText.Length)
    {
      char c = codeText[endAssign];
      if (IsStringStart(codeText, endAssign))
      {
        endAssign = SkipStringLiteral(codeText, endAssign);
        continue;
      }
      if (c == '(') parenCount++;
      else if (c == ')') parenCount--;
      else if (c == '{') curlyCount++;
      else if (c == '}') curlyCount--;

      if (parenCount < 0 || curlyCount < 0)
        break;

      if (c == ';' && parenCount == 0 && curlyCount == 0)
        break;

      if (c == '\n' && parenCount == 0 && curlyCount == 0)
        break;

      endAssign++;
    }

    string valueExpr = codeText.Substring(scan, endAssign - scan).Trim();

    string replacement = "";
    if (op == "=")
    {
      replacement = $"scr_tenna_flag_set({indexExpr}, {valueExpr})";
    }
    else if (op == "++")
    {
      replacement = $"scr_tenna_flag_set({indexExpr}, global.flag[{indexExpr}] + 1)";
    }
    else if (op == "--")
    {
      replacement = $"scr_tenna_flag_set({indexExpr}, global.flag[{indexExpr}] - 1)";
    }
    else if (op == "+=")
    {
      replacement = $"scr_tenna_flag_set({indexExpr}, global.flag[{indexExpr}] + ({valueExpr}))";
    }
    else if (op == "-=")
    {
      replacement = $"scr_tenna_flag_set({indexExpr}, global.flag[{indexExpr}] - ({valueExpr}))";
    }
    else if (op == "*=")
    {
      replacement = $"scr_tenna_flag_set({indexExpr}, global.flag[{indexExpr}] * ({valueExpr}))";
    }
    else if (op == "/=")
    {
      replacement = $"scr_tenna_flag_set({indexExpr}, global.flag[{indexExpr}] / ({valueExpr}))";
    }

    int originalLength = (endAssign + (endAssign < codeText.Length && codeText[endAssign] == ';' ? 1 : 0)) - index;
    string fullReplacement = replacement + (endAssign < codeText.Length && codeText[endAssign] == ';' ? ";" : "");

    codeText = codeText.Substring(0, index) + fullReplacement + codeText.Substring(index + originalLength);

    index += fullReplacement.Length;
  }
  return codeText;
}

int FindNextCodeGlobalFlag(string source, int startIndex)
{
  int scan = Math.Max(startIndex, 0);
  while (scan < source.Length)
  {
    if (IsStringStart(source, scan))
    {
      scan = SkipStringLiteral(source, scan);
      continue;
    }

    if (scan + 1 < source.Length && source[scan] == '/' && source[scan + 1] == '/')
    {
      scan = SkipLineComment(source, scan);
      continue;
    }

    if (scan + 1 < source.Length && source[scan] == '/' && source[scan + 1] == '*')
    {
      scan = SkipBlockComment(source, scan);
      continue;
    }

    if (scan + "global.flag".Length <= source.Length && source.Substring(scan, "global.flag".Length) == "global.flag")
      return scan;

    scan++;
  }

  return -1;
}

bool IsStringStart(string source, int index)
{
  return index < source.Length && (source[index] == '"' || source[index] == '\'');
}

int SkipStringLiteral(string source, int quoteIndex)
{
  char quote = source[quoteIndex];
  int scan = quoteIndex + 1;
  while (scan < source.Length)
  {
    if (source[scan] == '\\')
    {
      scan += 2;
      continue;
    }

    if (source[scan] == quote)
      return scan + 1;

    scan++;
  }

  return source.Length;
}

int SkipLineComment(string source, int commentIndex)
{
  int newline = source.IndexOf('\n', commentIndex + 2);
  return newline < 0 ? source.Length : newline + 1;
}

int SkipBlockComment(string source, int commentIndex)
{
  int end = source.IndexOf("*/", commentIndex + 2, StringComparison.Ordinal);
  return end < 0 ? source.Length : end + 2;
}

string TennaCleanBlock(string source, string startPattern, string endPattern)
{
  int startIdx = source.IndexOf(startPattern, StringComparison.Ordinal);
  if (startIdx < 0)
    return source;

  int ifIdx = source.LastIndexOf("if", startIdx, StringComparison.Ordinal);
  if (ifIdx >= 0 && startIdx - ifIdx < 15)
    startIdx = ifIdx;

  int endIdx = source.IndexOf(endPattern, startIdx, StringComparison.Ordinal);
  if (endIdx < 0)
    return source;

  endIdx += endPattern.Length;

  int braceCount = 0;
  while (endIdx < source.Length)
  {
    char c = source[endIdx];
    if (c == '\r' || c == '\n' || c == ' ')
    {
      endIdx++;
    }
    else if (c == '}' && braceCount < 3)
    {
      endIdx++;
      braceCount++;
    }
    else
    {
      break;
    }
  }

  return source.Substring(0, startIdx) + source.Substring(endIdx);
}

string TennaCleanAllBlocks(string source, string startPattern, string endPattern)
{
  string current = source;
  while (true)
  {
    string cleaned = TennaCleanBlock(current, startPattern, endPattern);
    if (cleaned == current)
      break;
    current = cleaned;
  }
  return current;
}

string TennaCleanBraceBlock(string source, string startPattern)
{
  int startIdx = source.IndexOf(startPattern, StringComparison.Ordinal);
  if (startIdx < 0)
    return source;

  int ifIdx = source.LastIndexOf("if", startIdx, StringComparison.Ordinal);
  if (ifIdx >= 0 && startIdx - ifIdx < 15)
    startIdx = ifIdx;

  int braceIdx = source.IndexOf("{", startIdx, StringComparison.Ordinal);
  if (braceIdx < 0)
    return source;

  int level = 1;
  int scanIdx = braceIdx + 1;
  while (scanIdx < source.Length && level > 0)
  {
    char c = source[scanIdx];
    if (c == '{')
      level++;
    else if (c == '}')
      level--;
    scanIdx++;
  }

  if (level == 0)
  {
    int endIdx = scanIdx;
    while (endIdx < source.Length && (source[endIdx] == '\r' || source[endIdx] == '\n' || source[endIdx] == ' '))
    {
      endIdx++;
    }
    return source.Substring(0, startIdx) + source.Substring(endIdx);
  }

  return source;
}

string TennaCleanAllBraceBlocks(string source, string startPattern)
{
  string current = source;
  while (true)
  {
    string cleaned = TennaCleanBraceBlock(current, startPattern);
    if (cleaned == current)
      break;
    current = cleaned;
  }
  return current;
}

bool ReferencesFlag(UndertaleCode code)
{
  if (code.Instructions == null)
    return false;
  foreach (var instr in code.Instructions)
  {
    if (instr.Kind == UndertaleInstruction.Opcode.Pop && instr.ValueVariable is UndertaleVariable v && v.Name?.Content == "flag")
      return true;
  }
  return false;
}
