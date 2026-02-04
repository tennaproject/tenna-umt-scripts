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
if (checkCreate.Contains("_tenna_core_enabled"))
{
  ScriptError("Tenna Core is already installed!");
  return;
}

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
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

global._tenna_core_filename = ""tenna-"" + _ts + "".txt"";
global._tenna_core_ts = _ts;

global._tenna_core_ver = """";
if (variable_global_exists(""versionno""))
    global._tenna_core_ver = string(global.versionno);
if (variable_global_exists(""version""))
    global._tenna_core_ver = string(global.version);

var _f = file_text_open_write(global._tenna_core_filename);
file_text_write_string(_f, ""Tenna Core "" + global._tenna_core_ver + "" "" + _ts);
file_text_writeln(_f);
file_text_writeln(_f);
file_text_close(_f);
";

string stepCheck = @"
if (keyboard_check_pressed(ord(""1"")) && keyboard_check(vk_alt))
    global._tenna_core_visible = !global._tenna_core_visible;
";

string drawDisplay = @"
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
";

var logFuncName = "scr_tenna_log";
if (Data.Scripts.ByName(logFuncName) is null)
{
  var logCodeName = "gml_Script_" + logFuncName;
  var logCode = new UndertaleCode { Name = Data.Strings.MakeString(logCodeName) };
  Data.Code.Add(logCode);
  var logScript = new UndertaleScript { Name = Data.Strings.MakeString(logFuncName), Code = logCode };
  Data.Scripts.Add(logScript);
  
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
}

try
{
  importGroup.QueueReplace(createCode, GetDecompiledText(createCode) + createInit);
  importGroup.QueueReplace(stepCode, GetDecompiledText(stepCode) + stepCheck);
  importGroup.QueueReplace(drawCode, GetDecompiledText(drawCode) + drawDisplay);
  
  importGroup.Import();
  ScriptMessage("Tenna Core installed!\n\nAlt+1 to toggle display.");
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
}
