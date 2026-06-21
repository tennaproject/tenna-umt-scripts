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

string checkCreate = GetDecompiledText(createCode);

if (!checkCreate.Contains("_tenna_core_enabled"))
{
  ScriptError("Tenna Core is required!\n\nPlease install GameCore.csx first.");
  return;
}

bool stateDumpAlreadyInstalled = checkCreate.Contains("_tenna_sd_enabled");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
global._tenna_sd_enabled = true;
directory_create(""tenna"");
directory_create(""tenna/state"");
";

string stepCheck = @"
if (keyboard_check_pressed(ord(""5"")) && keyboard_check(vk_alt))
    scr_tenna_state_dump();
";

var dumpFuncName = "scr_tenna_state_dump";
UndertaleCode dumpCode;
if (Data.Scripts.ByName(dumpFuncName)?.Code is UndertaleCode existingDumpCode)
{
  dumpCode = existingDumpCode;
}
else
{
  var dumpCodeName = "gml_Script_" + dumpFuncName;
  dumpCode = new UndertaleCode { Name = Data.Strings.MakeString(dumpCodeName) };
  Data.Code.Add(dumpCode);
  var dumpScript = new UndertaleScript { Name = Data.Strings.MakeString(dumpFuncName), Code = dumpCode };
  Data.Scripts.Add(dumpScript);
}

string dumpFuncBody = @"
var _ts = string(current_year) + ""-"" 
    + string_format(current_month, 2, 0) + ""-"" 
    + string_format(current_day, 2, 0) + ""_"" 
    + string_format(current_hour, 2, 0) + ""-"" 
    + string_format(current_minute, 2, 0) + ""-"" 
    + string_format(current_second, 2, 0);
_ts = string_replace_all(_ts, "" "", ""0"");

directory_create(""tenna"");
directory_create(""tenna/state"");

var _file = ""tenna/state/state-"" + _ts + "".json"";
var _f = file_text_open_write(_file);
var _q = chr(34);

file_text_write_string(_f, ""{"");
file_text_writeln(_f);

file_text_write_string(_f, ""  "" + _q + ""timestamp"" + _q + "": "" + _q + _ts + _q + "","");
file_text_writeln(_f);
file_text_write_string(_f, ""  "" + _q + ""chapter"" + _q + "": "" + string(global.chapter) + "","");
file_text_writeln(_f);
file_text_write_string(_f, ""  "" + _q + ""room"" + _q + "": "" + string(global.currentroom) + "","");
file_text_writeln(_f);
file_text_write_string(_f, ""  "" + _q + ""plot"" + _q + "": "" + string(global.plot) + "","");
file_text_writeln(_f);
file_text_write_string(_f, ""  "" + _q + ""darkDollars"" + _q + "": "" + string(global.gold) + "","");
file_text_writeln(_f);
file_text_write_string(_f, ""  "" + _q + ""lightWorldMoney"" + _q + "": "" + string(global.lgold) + "","");
file_text_writeln(_f);

file_text_write_string(_f, ""  "" + _q + ""party"" + _q + "": ["");
file_text_writeln(_f);
for (var _i = 0; _i < 5; _i++)
{
    var _character = 0;
    if (_i < 3)
        _character = global.char[_i];
    file_text_write_string(_f, ""    {"" + _q + ""slot"" + _q + "": "" + string(_i));
    file_text_write_string(_f, "", "" + _q + ""character"" + _q + "": "" + string(_character));
    file_text_write_string(_f, "", "" + _q + ""hp"" + _q + "": "" + string(global.hp[_i]));
    file_text_write_string(_f, "", "" + _q + ""maxHp"" + _q + "": "" + string(global.maxhp[_i]));
    file_text_write_string(_f, "", "" + _q + ""at"" + _q + "": "" + string(global.at[_i]));
    file_text_write_string(_f, "", "" + _q + ""df"" + _q + "": "" + string(global.df[_i]));
    file_text_write_string(_f, "", "" + _q + ""mag"" + _q + "": "" + string(global.mag[_i]));
    file_text_write_string(_f, "", "" + _q + ""weapon"" + _q + "": "" + string(global.charweapon[_i]));
    file_text_write_string(_f, "", "" + _q + ""armor1"" + _q + "": "" + string(global.chararmor1[_i]));
    file_text_write_string(_f, "", "" + _q + ""armor2"" + _q + "": "" + string(global.chararmor2[_i]) + ""}"");
    if (_i < 4)
        file_text_write_string(_f, "","");
    file_text_writeln(_f);
}
file_text_write_string(_f, ""  ],"");
file_text_writeln(_f);

file_text_write_string(_f, ""  "" + _q + ""inventory"" + _q + "": {"");
file_text_writeln(_f);
scr_tenna_sd_write_array(_f, ""consumables"", ""item"", 13, true);
scr_tenna_sd_write_array(_f, ""keyItems"", ""keyitem"", 13, true);
scr_tenna_sd_write_array(_f, ""weapons"", ""weapon"", 48, true);
scr_tenna_sd_write_array(_f, ""armors"", ""armor"", 48, true);
scr_tenna_sd_write_array(_f, ""pocketItems"", ""pocketitem"", 72, true);
scr_tenna_sd_write_array(_f, ""lightWorldItems"", ""litem"", 8, true);
scr_tenna_sd_write_array(_f, ""phoneContacts"", ""phone"", 8, false);
file_text_write_string(_f, ""  },"");
file_text_writeln(_f);

file_text_write_string(_f, ""  "" + _q + ""equippedLightWorld"" + _q + "": {"" + _q + ""weapon"" + _q + "": "" + string(global.lweapon) + "", "" + _q + ""armor"" + _q + "": "" + string(global.larmor) + ""},"");
file_text_writeln(_f);

file_text_write_string(_f, ""  "" + _q + ""spells"" + _q + "": ["");
file_text_writeln(_f);
for (var _i = 0; _i < 5; _i++)
{
    file_text_write_string(_f, ""    {"" + _q + ""slot"" + _q + "": "" + string(_i) + "", "" + _q + ""values"" + _q + "": ["");
    for (var _j = 0; _j < 12; _j++)
    {
        if (_j > 0)
            file_text_write_string(_f, "", "");
        file_text_write_string(_f, string(scr_tenna_sd_get_global_2d(""spell"", _i, _j)));
    }
    file_text_write_string(_f, ""]}"");
    if (_i < 4)
        file_text_write_string(_f, "","");
    file_text_writeln(_f);
}
file_text_write_string(_f, ""  ],"");
file_text_writeln(_f);

file_text_write_string(_f, ""  "" + _q + ""flagsNonZero"" + _q + "": ["");
var _first = true;
for (var _i = 0; _i < 2500; _i++)
{
    if (global.flag[_i] != 0)
    {
        if (!_first)
            file_text_write_string(_f, "", "");
        file_text_write_string(_f, ""{"" + _q + ""id"" + _q + "": "" + string(_i) + "", "" + _q + ""value"" + _q + "": "" + string(global.flag[_i]) + ""}"");
        _first = false;
    }
}
file_text_write_string(_f, ""]"");
file_text_writeln(_f);

file_text_write_string(_f, ""}"");
file_text_writeln(_f);
file_text_close(_f);

scr_tenna_log(""StateDump"", ""Exported current state to "" + _file);
";
importGroup.QueueReplace(dumpCode, dumpFuncBody);

var getGlobal2dFuncName = "scr_tenna_sd_get_global_2d";
UndertaleCode getGlobal2dCode;
if (Data.Scripts.ByName(getGlobal2dFuncName)?.Code is UndertaleCode existingGetGlobal2dCode)
{
  getGlobal2dCode = existingGetGlobal2dCode;
}
else
{
  var getGlobal2dCodeName = "gml_Script_" + getGlobal2dFuncName;
  getGlobal2dCode = new UndertaleCode { Name = Data.Strings.MakeString(getGlobal2dCodeName) };
  Data.Code.Add(getGlobal2dCode);
  var getGlobal2dScript = new UndertaleScript { Name = Data.Strings.MakeString(getGlobal2dFuncName), Code = getGlobal2dCode };
  Data.Scripts.Add(getGlobal2dScript);
}

string getGlobal2dFuncBody = @"
var _name = argument0;
var _y = argument1;
var _x = argument2;

if (!variable_global_exists(_name))
    return 0;

var _array = variable_global_get(_name);
if (_y < 0 || _x < 0)
    return 0;
if (!is_array(_array))
    return 0;
if (_y >= array_height_2d(_array))
    return 0;
if (_x >= array_length_2d(_array, _y))
    return 0;

return _array[_y][_x];
";
importGroup.QueueReplace(getGlobal2dCode, getGlobal2dFuncBody);

var writeArrayFuncName = "scr_tenna_sd_write_array";
UndertaleCode writeArrayCode;
if (Data.Scripts.ByName(writeArrayFuncName)?.Code is UndertaleCode existingWriteArrayCode)
{
  writeArrayCode = existingWriteArrayCode;
}
else
{
  var writeArrayCodeName = "gml_Script_" + writeArrayFuncName;
  writeArrayCode = new UndertaleCode { Name = Data.Strings.MakeString(writeArrayCodeName) };
  Data.Code.Add(writeArrayCode);
  var writeArrayScript = new UndertaleScript { Name = Data.Strings.MakeString(writeArrayFuncName), Code = writeArrayCode };
  Data.Scripts.Add(writeArrayScript);
}

string writeArrayFuncBody = @"
var _f = argument0;
var _json_name = argument1;
var _global_name = argument2;
var _size = argument3;
var _comma = argument4;

var _q = chr(34);
file_text_write_string(_f, ""    "" + _q + _json_name + _q + "": ["");
for (var _i = 0; _i < _size; _i++)
{
    if (_i > 0)
        file_text_write_string(_f, "", "");
    
    var _value = 0;
    if (_global_name == ""item"")
        _value = global.item[_i];
    else if (_global_name == ""keyitem"")
        _value = global.keyitem[_i];
    else if (_global_name == ""weapon"")
        _value = global.weapon[_i];
    else if (_global_name == ""armor"")
        _value = global.armor[_i];
    else if (_global_name == ""pocketitem"")
        _value = global.pocketitem[_i];
    else if (_global_name == ""litem"")
        _value = global.litem[_i];
    else if (_global_name == ""phone"")
        _value = global.phone[_i];
    
    file_text_write_string(_f, string(_value));
}
file_text_write_string(_f, ""]"");
if (_comma)
    file_text_write_string(_f, "","");
file_text_writeln(_f);
";
importGroup.QueueReplace(writeArrayCode, writeArrayFuncBody);

try
{
  if (!stateDumpAlreadyInstalled)
  {
    importGroup.QueueReplace(createCode, GetDecompiledText(createCode) + createInit);
    importGroup.QueueReplace(stepCode, GetDecompiledText(stepCode) + stepCheck);
  }
  
  importGroup.Import();
  if (Environment.GetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES") != "1")
    ScriptMessage("State Dump " + (stateDumpAlreadyInstalled ? "updated" : "installed") + "!\n\nAlt+5 exports current save-relevant state to tenna/state/.");
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
}
