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
bool coreAlreadyInstalled = checkCreate.Contains("_tenna_core_enabled");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
// TENNA_CORE_CREATE_BEGIN
if (instance_number(obj_time) > 1)
    exit;

global._tenna_core_enabled = true;
global._tenna_core_visible = true;
global._tenna_core_start_time = current_time;

var _ts = string(current_year) + ""-"" 
    + string_format(current_month, 2, 0) + ""-"" 
    + string_format(current_day, 2, 0) + ""_"" 
    + string_format(current_hour, 2, 0) + ""-"" 
    + string_format(current_minute, 2, 0) + ""-"" 
    + string_format(current_second, 2, 0);
_ts = string_replace_all(_ts, "" "", ""0"");

global._tenna_core_filename = ""tenna/logs/tenna-"" + _ts + "".txt"";
global._tenna_core_ts = _ts;

global._tenna_core_ver = """";
if (variable_global_exists(""versionno""))
    global._tenna_core_ver = string(global.versionno);
if (variable_global_exists(""version""))
    global._tenna_core_ver = string(global.version);

directory_create(""tenna"");
directory_create(""tenna/logs"");

var _f = file_text_open_write(global._tenna_core_filename);
file_text_write_string(_f, ""Tenna Core "" + global._tenna_core_ver + "" "" + _ts);
file_text_writeln(_f);
file_text_writeln(_f);
file_text_close(_f);
// TENNA_CORE_CREATE_END
";

string stepCheck = @"
// TENNA_CORE_STEP_BEGIN
if (keyboard_check_pressed(ord(""1"")) && keyboard_check(vk_alt))
    global._tenna_core_visible = !global._tenna_core_visible;
// TENNA_CORE_STEP_END
";

string drawDisplay = @"
// TENNA_CORE_DRAW_BEGIN
if (global._tenna_core_visible)
{
    var _tenna_elapsed = (current_time - global._tenna_core_start_time) / 1000;
    var _tenna_mins = floor(_tenna_elapsed / 60);
    var _tenna_secs = floor(_tenna_elapsed) mod 60;
    var _tenna_time = string(_tenna_mins) + "":"" + ((_tenna_secs < 10) ? ""0"" : """") + string(_tenna_secs);
    var _tenna_display = ""Tenna Core ("" + global._tenna_core_ver + "") ["" + global._tenna_core_ts + ""] ["" + _tenna_time + ""]"";
    
    draw_set_font(fnt_small);
    draw_set_halign(fa_right);
    draw_set_valign(fa_bottom);
    draw_set_color(c_black);
    draw_text(639, 479, _tenna_display);
    draw_set_color(c_white);
    draw_text(638, 478, _tenna_display);
    draw_set_halign(fa_left);
    draw_set_valign(fa_top);
}
// TENNA_CORE_DRAW_END
";

var logFuncName = "scr_tenna_log";
UndertaleCode logCode;
if (Data.Scripts.ByName(logFuncName)?.Code is UndertaleCode existingLogCode)
{
  logCode = existingLogCode;
}
else
{
  var logCodeName = "gml_Script_" + logFuncName;
  logCode = new UndertaleCode { Name = Data.Strings.MakeString(logCodeName) };
  Data.Code.Add(logCode);
  var logScript = new UndertaleScript { Name = Data.Strings.MakeString(logFuncName), Code = logCode };
  Data.Scripts.Add(logScript);
}

string logFuncBody = @"
var _prefix = argument0;
var _msg = argument1;

if (!global._tenna_core_enabled)
    return;

var _tenna_elapsed = (current_time - global._tenna_core_start_time) / 1000;
var _tenna_mins = floor(_tenna_elapsed / 60);
var _tenna_secs = floor(_tenna_elapsed) mod 60;
var _tenna_ts = string(_tenna_mins) + "":"" + ((_tenna_secs < 10) ? ""0"" : """") + string(_tenna_secs);

var _f = file_text_open_append(global._tenna_core_filename);
file_text_write_string(_f, ""["" + _tenna_ts + ""] ["" + _prefix + ""] "" + _msg);
file_text_writeln(_f);
file_text_close(_f);
";
importGroup.QueueReplace(logCode, logFuncBody);

try
{
  string currentCreateText = GetDecompiledText(createCode);
  string currentStepText = GetDecompiledText(stepCode);
  string currentDrawText = GetDecompiledText(drawCode);

  string cleanCreate = TennaCleanAllBlocks(currentCreateText, "global._tenna_core_enabled = true;", "file_text_close(_f);");
  importGroup.QueueReplace(createCode, cleanCreate + createInit);

  string cleanStep = TennaCleanAllBlocks(currentStepText, "keyboard_check_pressed(ord(\"1\"))", "global._tenna_core_visible = !global._tenna_core_visible;");
  importGroup.QueueReplace(stepCode, stepCheck + cleanStep);

  string cleanDraw = TennaCleanAllBraceBlocks(currentDrawText, "global._tenna_core_visible");
  importGroup.QueueReplace(drawCode, cleanDraw + drawDisplay);
  
  importGroup.Import();
  if (Environment.GetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES") != "1")
    ScriptMessage("Tenna Core " + (coreAlreadyInstalled ? "updated" : "installed") + "!\n\nAlt+1 to toggle display.");
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
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

