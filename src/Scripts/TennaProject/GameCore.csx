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

scr_tenna_config_load();
global._tenna_core_visible = global._tenna_config_ui_core_visible;

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
{
    global._tenna_core_visible = !global._tenna_core_visible;
    scr_tenna_config_set_core_visible(global._tenna_core_visible);
}
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

var configLoadFuncName = "scr_tenna_config_load";
UndertaleCode configLoadCode;
if (Data.Scripts.ByName(configLoadFuncName)?.Code is UndertaleCode existingConfigLoadCode)
{
  configLoadCode = existingConfigLoadCode;
}
else
{
  var configLoadCodeName = "gml_Script_" + configLoadFuncName;
  configLoadCode = new UndertaleCode { Name = Data.Strings.MakeString(configLoadCodeName) };
  Data.Code.Add(configLoadCode);
  var configLoadScript = new UndertaleScript { Name = Data.Strings.MakeString(configLoadFuncName), Code = configLoadCode };
  Data.Scripts.Add(configLoadScript);
}

string configLoadFuncBody = @"
global._tenna_config_path = ""tenna/config.json"";
global._tenna_config_ui_core_visible = true;
global._tenna_config_ui_flag_watcher_visible = true;
global._tenna_config_ui_plot_watcher_visible = true;
global._tenna_config_flag_watcher_ignored = [6, 20, 21, 33];

directory_create(""tenna"");

var _text = """";
if (file_exists(global._tenna_config_path))
{
    var _f = file_text_open_read(global._tenna_config_path);
    while (!file_text_eof(_f))
    {
        _text += file_text_read_string(_f);
        file_text_readln(_f);
    }
    file_text_close(_f);
}

if (_text != """")
{
    global._tenna_config_ui_core_visible = scr_tenna_config_read_bool(_text, ""coreVisible"", global._tenna_config_ui_core_visible);
    global._tenna_config_ui_flag_watcher_visible = scr_tenna_config_read_bool(_text, ""flagWatcherVisible"", global._tenna_config_ui_flag_watcher_visible);
    global._tenna_config_ui_plot_watcher_visible = scr_tenna_config_read_bool(_text, ""plotWatcherVisible"", global._tenna_config_ui_plot_watcher_visible);
    global._tenna_config_flag_watcher_ignored = scr_tenna_config_read_number_array(_text, ""ignoredFlags"", global._tenna_config_flag_watcher_ignored);
}

if (!file_exists(global._tenna_config_path))
    scr_tenna_config_save();
";
importGroup.QueueReplace(configLoadCode, configLoadFuncBody);

var configSaveFuncName = "scr_tenna_config_save";
UndertaleCode configSaveCode;
if (Data.Scripts.ByName(configSaveFuncName)?.Code is UndertaleCode existingConfigSaveCode)
{
  configSaveCode = existingConfigSaveCode;
}
else
{
  var configSaveCodeName = "gml_Script_" + configSaveFuncName;
  configSaveCode = new UndertaleCode { Name = Data.Strings.MakeString(configSaveCodeName) };
  Data.Code.Add(configSaveCode);
  var configSaveScript = new UndertaleScript { Name = Data.Strings.MakeString(configSaveFuncName), Code = configSaveCode };
  Data.Scripts.Add(configSaveScript);
}

string configSaveFuncBody = @"
directory_create(""tenna"");

var _ignored = """";
for (var _i = 0; _i < array_length(global._tenna_config_flag_watcher_ignored); _i++)
{
    if (_i > 0)
        _ignored += "", "";
    _ignored += string(global._tenna_config_flag_watcher_ignored[_i]);
}

var _f = file_text_open_write(global._tenna_config_path);
file_text_write_string(_f, ""{"");
file_text_writeln(_f);
file_text_write_string(_f, ""  "" + chr(34) + ""ui"" + chr(34) + "": {"");
file_text_writeln(_f);
file_text_write_string(_f, ""    "" + chr(34) + ""coreVisible"" + chr(34) + "": "" + (global._tenna_config_ui_core_visible ? ""true"" : ""false"") + "","");
file_text_writeln(_f);
file_text_write_string(_f, ""    "" + chr(34) + ""flagWatcherVisible"" + chr(34) + "": "" + (global._tenna_config_ui_flag_watcher_visible ? ""true"" : ""false"") + "","");
file_text_writeln(_f);
file_text_write_string(_f, ""    "" + chr(34) + ""plotWatcherVisible"" + chr(34) + "": "" + (global._tenna_config_ui_plot_watcher_visible ? ""true"" : ""false""));
file_text_writeln(_f);
file_text_write_string(_f, ""  },"");
file_text_writeln(_f);
file_text_write_string(_f, ""  "" + chr(34) + ""flagWatcher"" + chr(34) + "": {"");
file_text_writeln(_f);
file_text_write_string(_f, ""    "" + chr(34) + ""ignoredFlags"" + chr(34) + "": ["" + _ignored + ""]"");
file_text_writeln(_f);
file_text_write_string(_f, ""  }"");
file_text_writeln(_f);
file_text_write_string(_f, ""}"");
file_text_writeln(_f);
file_text_close(_f);
";
importGroup.QueueReplace(configSaveCode, configSaveFuncBody);

var configReadBoolFuncName = "scr_tenna_config_read_bool";
UndertaleCode configReadBoolCode;
if (Data.Scripts.ByName(configReadBoolFuncName)?.Code is UndertaleCode existingConfigReadBoolCode)
{
  configReadBoolCode = existingConfigReadBoolCode;
}
else
{
  var configReadBoolCodeName = "gml_Script_" + configReadBoolFuncName;
  configReadBoolCode = new UndertaleCode { Name = Data.Strings.MakeString(configReadBoolCodeName) };
  Data.Code.Add(configReadBoolCode);
  var configReadBoolScript = new UndertaleScript { Name = Data.Strings.MakeString(configReadBoolFuncName), Code = configReadBoolCode };
  Data.Scripts.Add(configReadBoolScript);
}

string configReadBoolFuncBody = @"
var _text = argument0;
var _key = argument1;
var _default = argument2;
var _needle = chr(34) + _key + chr(34);
var _pos = string_pos(_needle, _text);
if (_pos <= 0)
    return _default;

var _colon = string_pos_ext("":"", _text, _pos + string_length(_needle));
if (_colon <= 0)
    return _default;

var _rest = string_lower(string_copy(_text, _colon + 1, 8));
if (string_pos(""true"", _rest) == 1)
    return true;
if (string_pos(""false"", _rest) == 1)
    return false;
return _default;
";
importGroup.QueueReplace(configReadBoolCode, configReadBoolFuncBody);

var configReadNumberArrayFuncName = "scr_tenna_config_read_number_array";
UndertaleCode configReadNumberArrayCode;
if (Data.Scripts.ByName(configReadNumberArrayFuncName)?.Code is UndertaleCode existingConfigReadNumberArrayCode)
{
  configReadNumberArrayCode = existingConfigReadNumberArrayCode;
}
else
{
  var configReadNumberArrayCodeName = "gml_Script_" + configReadNumberArrayFuncName;
  configReadNumberArrayCode = new UndertaleCode { Name = Data.Strings.MakeString(configReadNumberArrayCodeName) };
  Data.Code.Add(configReadNumberArrayCode);
  var configReadNumberArrayScript = new UndertaleScript { Name = Data.Strings.MakeString(configReadNumberArrayFuncName), Code = configReadNumberArrayCode };
  Data.Scripts.Add(configReadNumberArrayScript);
}

string configReadNumberArrayFuncBody = @"
var _text = argument0;
var _key = argument1;
var _default = argument2;
var _needle = chr(34) + _key + chr(34);
var _pos = string_pos(_needle, _text);
if (_pos <= 0)
    return _default;

var _open = string_pos_ext(""["", _text, _pos + string_length(_needle));
if (_open <= 0)
    return _default;

var _close = string_pos_ext(""]"", _text, _open + 1);
if (_close <= 0)
    return _default;

var _result = [];
var _token = """";
for (var _i = _open + 1; _i < _close; _i++)
{
    var _char = string_char_at(_text, _i);
    if ((_char >= ""0"" && _char <= ""9"") || _char == ""-"")
    {
        _token += _char;
    }
    else if (_token != """")
    {
        array_push(_result, real(_token));
        _token = """";
    }
}

if (_token != """")
    array_push(_result, real(_token));

if (array_length(_result) == 0)
    return _default;
return _result;
";
importGroup.QueueReplace(configReadNumberArrayCode, configReadNumberArrayFuncBody);

var configSetCoreVisibleFuncName = "scr_tenna_config_set_core_visible";
UndertaleCode configSetCoreVisibleCode;
if (Data.Scripts.ByName(configSetCoreVisibleFuncName)?.Code is UndertaleCode existingConfigSetCoreVisibleCode)
{
  configSetCoreVisibleCode = existingConfigSetCoreVisibleCode;
}
else
{
  var configSetCoreVisibleCodeName = "gml_Script_" + configSetCoreVisibleFuncName;
  configSetCoreVisibleCode = new UndertaleCode { Name = Data.Strings.MakeString(configSetCoreVisibleCodeName) };
  Data.Code.Add(configSetCoreVisibleCode);
  var configSetCoreVisibleScript = new UndertaleScript { Name = Data.Strings.MakeString(configSetCoreVisibleFuncName), Code = configSetCoreVisibleCode };
  Data.Scripts.Add(configSetCoreVisibleScript);
}

string configSetCoreVisibleFuncBody = @"
global._tenna_config_ui_core_visible = argument0;
scr_tenna_config_save();
";
importGroup.QueueReplace(configSetCoreVisibleCode, configSetCoreVisibleFuncBody);

var configSetFlagWatcherVisibleFuncName = "scr_tenna_config_set_flag_watcher_visible";
UndertaleCode configSetFlagWatcherVisibleCode;
if (Data.Scripts.ByName(configSetFlagWatcherVisibleFuncName)?.Code is UndertaleCode existingConfigSetFlagWatcherVisibleCode)
{
  configSetFlagWatcherVisibleCode = existingConfigSetFlagWatcherVisibleCode;
}
else
{
  var configSetFlagWatcherVisibleCodeName = "gml_Script_" + configSetFlagWatcherVisibleFuncName;
  configSetFlagWatcherVisibleCode = new UndertaleCode { Name = Data.Strings.MakeString(configSetFlagWatcherVisibleCodeName) };
  Data.Code.Add(configSetFlagWatcherVisibleCode);
  var configSetFlagWatcherVisibleScript = new UndertaleScript { Name = Data.Strings.MakeString(configSetFlagWatcherVisibleFuncName), Code = configSetFlagWatcherVisibleCode };
  Data.Scripts.Add(configSetFlagWatcherVisibleScript);
}

string configSetFlagWatcherVisibleFuncBody = @"
global._tenna_config_ui_flag_watcher_visible = argument0;
scr_tenna_config_save();
";
importGroup.QueueReplace(configSetFlagWatcherVisibleCode, configSetFlagWatcherVisibleFuncBody);

var configSetPlotWatcherVisibleFuncName = "scr_tenna_config_set_plot_watcher_visible";
UndertaleCode configSetPlotWatcherVisibleCode;
if (Data.Scripts.ByName(configSetPlotWatcherVisibleFuncName)?.Code is UndertaleCode existingConfigSetPlotWatcherVisibleCode)
{
  configSetPlotWatcherVisibleCode = existingConfigSetPlotWatcherVisibleCode;
}
else
{
  var configSetPlotWatcherVisibleCodeName = "gml_Script_" + configSetPlotWatcherVisibleFuncName;
  configSetPlotWatcherVisibleCode = new UndertaleCode { Name = Data.Strings.MakeString(configSetPlotWatcherVisibleCodeName) };
  Data.Code.Add(configSetPlotWatcherVisibleCode);
  var configSetPlotWatcherVisibleScript = new UndertaleScript { Name = Data.Strings.MakeString(configSetPlotWatcherVisibleFuncName), Code = configSetPlotWatcherVisibleCode };
  Data.Scripts.Add(configSetPlotWatcherVisibleScript);
}

string configSetPlotWatcherVisibleFuncBody = @"
global._tenna_config_ui_plot_watcher_visible = argument0;
scr_tenna_config_save();
";
importGroup.QueueReplace(configSetPlotWatcherVisibleCode, configSetPlotWatcherVisibleFuncBody);

var configFlagWatcherIgnoredFuncName = "scr_tenna_config_flag_watcher_ignored";
UndertaleCode configFlagWatcherIgnoredCode;
if (Data.Scripts.ByName(configFlagWatcherIgnoredFuncName)?.Code is UndertaleCode existingConfigFlagWatcherIgnoredCode)
{
  configFlagWatcherIgnoredCode = existingConfigFlagWatcherIgnoredCode;
}
else
{
  var configFlagWatcherIgnoredCodeName = "gml_Script_" + configFlagWatcherIgnoredFuncName;
  configFlagWatcherIgnoredCode = new UndertaleCode { Name = Data.Strings.MakeString(configFlagWatcherIgnoredCodeName) };
  Data.Code.Add(configFlagWatcherIgnoredCode);
  var configFlagWatcherIgnoredScript = new UndertaleScript { Name = Data.Strings.MakeString(configFlagWatcherIgnoredFuncName), Code = configFlagWatcherIgnoredCode };
  Data.Scripts.Add(configFlagWatcherIgnoredScript);
}

string configFlagWatcherIgnoredFuncBody = @"
var _flag = argument0;
if (!variable_global_exists(""_tenna_config_flag_watcher_ignored"") || !is_array(global._tenna_config_flag_watcher_ignored))
    return false;

for (var _i = 0; _i < array_length(global._tenna_config_flag_watcher_ignored); _i++)
{
    if (global._tenna_config_flag_watcher_ignored[_i] == _flag)
        return true;
}
return false;
";
importGroup.QueueReplace(configFlagWatcherIgnoredCode, configFlagWatcherIgnoredFuncBody);

var configToggleIgnoredFlagFuncName = "scr_tenna_config_toggle_ignored_flag";
UndertaleCode configToggleIgnoredFlagCode;
if (Data.Scripts.ByName(configToggleIgnoredFlagFuncName)?.Code is UndertaleCode existingConfigToggleIgnoredFlagCode)
{
  configToggleIgnoredFlagCode = existingConfigToggleIgnoredFlagCode;
}
else
{
  var configToggleIgnoredFlagCodeName = "gml_Script_" + configToggleIgnoredFlagFuncName;
  configToggleIgnoredFlagCode = new UndertaleCode { Name = Data.Strings.MakeString(configToggleIgnoredFlagCodeName) };
  Data.Code.Add(configToggleIgnoredFlagCode);
  var configToggleIgnoredFlagScript = new UndertaleScript { Name = Data.Strings.MakeString(configToggleIgnoredFlagFuncName), Code = configToggleIgnoredFlagCode };
  Data.Scripts.Add(configToggleIgnoredFlagScript);
}

string configToggleIgnoredFlagFuncBody = @"
var _flag = floor(argument0);
if (!variable_global_exists(""_tenna_config_flag_watcher_ignored"") || !is_array(global._tenna_config_flag_watcher_ignored))
    global._tenna_config_flag_watcher_ignored = [];

var _found = false;
var _next = [];
for (var _i = 0; _i < array_length(global._tenna_config_flag_watcher_ignored); _i++)
{
    var _candidate = floor(global._tenna_config_flag_watcher_ignored[_i]);
    if (_candidate == _flag)
    {
        _found = true;
    }
    else
    {
        _next[array_length(_next)] = _candidate;
    }
}

if (!_found)
    _next[array_length(_next)] = _flag;

global._tenna_config_flag_watcher_ignored = _next;
scr_tenna_config_save();
return !_found;
";
importGroup.QueueReplace(configToggleIgnoredFlagCode, configToggleIgnoredFlagFuncBody);

try
{
  string currentCreateText = GetDecompiledText(createCode);
  string currentStepText = GetDecompiledText(stepCode);
  string currentDrawText = GetDecompiledText(drawCode);

  string cleanCreate = TennaCleanAllBlocks(currentCreateText, "global._tenna_core_enabled = true;", "file_text_close(_f);");
  importGroup.QueueReplace(createCode, cleanCreate + createInit);

  string cleanStep = TennaCleanAllBlocks(currentStepText, "keyboard_check_pressed(ord(\"1\"))", "scr_tenna_config_set_core_visible(global._tenna_core_visible);");
  cleanStep = TennaCleanAllBlocks(cleanStep, "keyboard_check_pressed(ord(\"1\"))", "global._tenna_core_visible = !global._tenna_core_visible;");
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
