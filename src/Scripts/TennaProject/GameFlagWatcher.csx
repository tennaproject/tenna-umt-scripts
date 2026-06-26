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
if (Data.Scripts.ByName("scr_tenna_config_flag_watcher_ignored")?.Code is not UndertaleCode
  || Data.Scripts.ByName("scr_tenna_config_set_flag_watcher_visible")?.Code is not UndertaleCode
  || Data.Scripts.ByName("scr_tenna_config_toggle_ignored_flag")?.Code is not UndertaleCode)
{
  ScriptError("Tenna Core needs to be updated before installing Flag Watcher.\n\nPlease run GameCore.csx first.");
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
_tenna_fw_visible = global._tenna_config_ui_flag_watcher_visible;
directory_create(""tenna"");
directory_create(""tenna/flag-logs"");
global._tenna_fw_export_filename = ""tenna/flag-logs/flags-"" + global._tenna_core_ts + "".jsonl"";
global._tenna_loading_save = false;
global._tenna_fw_frame_writes = 0;
global._tenna_fw_last_flag = -1;

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

        if (scr_tenna_config_flag_watcher_ignored(_tenna_fw_i))
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
{
    _tenna_fw_visible = !_tenna_fw_visible;
    scr_tenna_config_set_flag_watcher_visible(_tenna_fw_visible);
}

if (keyboard_check_pressed(ord(""I"")) && keyboard_check(vk_alt) && global._tenna_fw_last_flag >= 0)
{
    var _tenna_fw_ignored = scr_tenna_config_toggle_ignored_flag(global._tenna_fw_last_flag);
    scr_tenna_log(""FlagWatcher"", ""Flag["" + string(global._tenna_fw_last_flag) + ""] "" + (_tenna_fw_ignored ? ""ignored"" : ""watched""));
    for (var _tenna_fw_j = _tenna_fw_max_log - 1; _tenna_fw_j > 0; _tenna_fw_j--)
    {
        _tenna_fw_log[_tenna_fw_j] = _tenna_fw_log[_tenna_fw_j - 1];
        _tenna_fw_alpha[_tenna_fw_j] = _tenna_fw_alpha[_tenna_fw_j - 1];
    }
    _tenna_fw_log[0] = ""Flag["" + string(global._tenna_fw_last_flag) + ""] "" + (_tenna_fw_ignored ? ""ignored"" : ""watched"");
    _tenna_fw_alpha[0] = 1;
}

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
var _has_bitmask = argument_count > 9;
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
if (_has_bitmask)
{
    ds_map_add(_row, ""bitIndex"", argument9);
    ds_map_add(_row, ""bitWidth"", argument10);
    if (argument9 >= 0)
    {
        ds_map_add(_row, ""oldBitValue"", argument11);
        ds_map_add(_row, ""newBitValue"", argument12);
    }
    else
    {
        if (argument_count > 13)
            ds_map_add(_row, ""changedBitIndices"", argument13);
        ds_map_add(_row, ""oldBitValues"", argument11);
        ds_map_add(_row, ""newBitValues"", argument12);
    }
}
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

if (scr_tenna_config_flag_watcher_ignored(_index))
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
        global._tenna_fw_last_flag = _index;
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

var flagSetBitmaskFunctionName = "scr_tenna_flag_set_bitmask";
UndertaleCode flagSetBitmaskCode;
if (Data.Scripts.ByName(flagSetBitmaskFunctionName)?.Code is UndertaleCode existingFlagSetBitmaskCode)
{
  flagSetBitmaskCode = existingFlagSetBitmaskCode;
}
else
{
  flagSetBitmaskCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + flagSetBitmaskFunctionName) };
  Data.Code.Add(flagSetBitmaskCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(flagSetBitmaskFunctionName), Code = flagSetBitmaskCode };
  Data.Scripts.Add(scriptEntry);
}

string flagSetBitmaskBody = @"
var _index = argument0;
var _bit_index = argument1;
var _bit_value = argument2;
var _bit_width = 1;
if (argument_count > 3)
    _bit_width = argument3;

if (_bit_index < 0)
    return scr_tenna_flag_set(_index, _bit_value);

var _len = 0;
if (variable_global_exists(""flag"") && is_array(global.flag))
    _len = array_length(global.flag);

var _old = 0;
if (_index < _len)
    _old = global.flag[_index];

var _max_bit_value = power(2, _bit_width) - 1;
var _bit_offset = _bit_index * _bit_width;
var _old_bit_value = (_old >> _bit_offset) & _max_bit_value;
var _clamped_bit_value = clamp(floor(_bit_value), 0, _max_bit_value);
var _value = _old;
_value &= ~(_max_bit_value << _bit_offset);
_value |= ((_clamped_bit_value & _max_bit_value) << _bit_offset);
var _new_bit_value = (_value >> _bit_offset) & _max_bit_value;

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

if (scr_tenna_config_flag_watcher_ignored(_index))
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

scr_tenna_fw_write_row(_tenna_fw_file, ""change"", _index, _old, _value, _tenna_fw_elapsed, _tenna_fw_chapter, _tenna_fw_room, _tenna_fw_plot, _bit_index, _bit_width, _old_bit_value, _new_bit_value);
file_text_close(_tenna_fw_file);

scr_tenna_log(""FlagWatcher"", ""["" + string(_index) + "":"" + string(_bit_index) + ""w"" + string(_bit_width) + ""]: "" + string(_old_bit_value) + "" -> "" + string(_new_bit_value) + "" parent="" + string(_old) + "" -> "" + string(_value) + "" room="" + string(_tenna_fw_room) + "" plot="" + string(_tenna_fw_plot));

if (instance_exists(obj_time))
{
    with (obj_time)
    {
        global._tenna_fw_last_flag = _index;
        for (var _tenna_fw_j = _tenna_fw_max_log - 1; _tenna_fw_j > 0; _tenna_fw_j--)
        {
            _tenna_fw_log[_tenna_fw_j] = _tenna_fw_log[_tenna_fw_j - 1];
            _tenna_fw_alpha[_tenna_fw_j] = _tenna_fw_alpha[_tenna_fw_j - 1];
        }
        _tenna_fw_log[0] = ""Flag["" + string(_index) + "":"" + string(_bit_index) + ""w"" + string(_bit_width) + ""]: "" + string(_old_bit_value) + "" -> "" + string(_new_bit_value);
        _tenna_fw_alpha[0] = 1;
    }
}
return 0;
";
importGroup.QueueReplace(flagSetBitmaskCode, flagSetBitmaskBody);

var flagSetBitmaskArrayFunctionName = "scr_tenna_flag_set_bitmask_array";
UndertaleCode flagSetBitmaskArrayCode;
if (Data.Scripts.ByName(flagSetBitmaskArrayFunctionName)?.Code is UndertaleCode existingFlagSetBitmaskArrayCode)
{
  flagSetBitmaskArrayCode = existingFlagSetBitmaskArrayCode;
}
else
{
  flagSetBitmaskArrayCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + flagSetBitmaskArrayFunctionName) };
  Data.Code.Add(flagSetBitmaskArrayCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(flagSetBitmaskArrayFunctionName), Code = flagSetBitmaskArrayCode };
  Data.Scripts.Add(scriptEntry);
}

string flagSetBitmaskArrayBody = @"
var _index = argument0;
var _values = argument1;
var _bit_width = 1;
if (argument_count > 2)
    _bit_width = argument2;

var _len = 0;
if (variable_global_exists(""flag"") && is_array(global.flag))
    _len = array_length(global.flag);

var _old = 0;
if (_index < _len)
    _old = global.flag[_index];

var _max_bit_value = power(2, _bit_width) - 1;
var _slot_count = min(array_length(_values), floor(18 / _bit_width));
var _value = 0;
var _changed_count = 0;
var _first_changed_index = -1;
var _first_old_bit_value = 0;
var _first_new_bit_value = 0;
var _changed_indices = """";
var _old_bit_values = """";
var _new_bit_values = """";

for (var _slot = 0; _slot < _slot_count; _slot++)
{
    var _raw_bit_value = _values[_slot];
    var _new_slot_value = clamp(floor(_raw_bit_value), 0, _max_bit_value);
    var _offset = _slot * _bit_width;
    var _old_slot_value = (_old >> _offset) & _max_bit_value;
    _value |= ((_new_slot_value & _max_bit_value) << _offset);

    if (_old_slot_value != _new_slot_value)
    {
        if (_changed_count > 0)
        {
            _changed_indices += "","";
            _old_bit_values += "","";
            _new_bit_values += "","";
        }
        _changed_indices += string(_slot);
        _old_bit_values += string(_old_slot_value);
        _new_bit_values += string(_new_slot_value);

        if (_changed_count == 0)
        {
            _first_changed_index = _slot;
            _first_old_bit_value = _old_slot_value;
            _first_new_bit_value = _new_slot_value;
        }
        _changed_count++;
    }
}

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

if (scr_tenna_config_flag_watcher_ignored(_index))
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

if (_changed_count == 1)
    scr_tenna_fw_write_row(_tenna_fw_file, ""change"", _index, _old, _value, _tenna_fw_elapsed, _tenna_fw_chapter, _tenna_fw_room, _tenna_fw_plot, _first_changed_index, _bit_width, _first_old_bit_value, _first_new_bit_value);
else
    scr_tenna_fw_write_row(_tenna_fw_file, ""change"", _index, _old, _value, _tenna_fw_elapsed, _tenna_fw_chapter, _tenna_fw_room, _tenna_fw_plot, -1, _bit_width, _old_bit_values, _new_bit_values, _changed_indices);
file_text_close(_tenna_fw_file);

if (_changed_count == 1)
    scr_tenna_log(""FlagWatcher"", ""["" + string(_index) + "":"" + string(_first_changed_index) + ""w"" + string(_bit_width) + ""]: "" + string(_first_old_bit_value) + "" -> "" + string(_first_new_bit_value) + "" parent="" + string(_old) + "" -> "" + string(_value) + "" room="" + string(_tenna_fw_room) + "" plot="" + string(_tenna_fw_plot));
else
    scr_tenna_log(""FlagWatcher"", ""["" + string(_index) + "":arrayw"" + string(_bit_width) + ""]: "" + string(_changed_count) + "" slots changed ("" + _changed_indices + "") parent="" + string(_old) + "" -> "" + string(_value) + "" room="" + string(_tenna_fw_room) + "" plot="" + string(_tenna_fw_plot));

if (instance_exists(obj_time))
{
    with (obj_time)
    {
        global._tenna_fw_last_flag = _index;
        for (var _tenna_fw_j = _tenna_fw_max_log - 1; _tenna_fw_j > 0; _tenna_fw_j--)
        {
            _tenna_fw_log[_tenna_fw_j] = _tenna_fw_log[_tenna_fw_j - 1];
            _tenna_fw_alpha[_tenna_fw_j] = _tenna_fw_alpha[_tenna_fw_j - 1];
        }
        if (_changed_count == 1)
            _tenna_fw_log[0] = ""Flag["" + string(_index) + "":"" + string(_first_changed_index) + ""w"" + string(_bit_width) + ""]: "" + string(_first_old_bit_value) + "" -> "" + string(_first_new_bit_value);
        else
            _tenna_fw_log[0] = ""Flag["" + string(_index) + "":arrayw"" + string(_bit_width) + ""]: "" + string(_changed_count) + "" slots changed"";
        _tenna_fw_alpha[0] = 1;
    }
}
return 0;
";
importGroup.QueueReplace(flagSetBitmaskArrayCode, flagSetBitmaskArrayBody);

bool flagSetExtUpdated = false;
List<UndertaleCode> flagSetExtCandidates = new List<UndertaleCode>();
if (Data.Code.ByName("gml_GlobalScript_scr_flag_set") is UndertaleCode globalFlagSetCode)
  flagSetExtCandidates.Add(globalFlagSetCode);
if (Data.Scripts.ByName("scr_flag_set_ext")?.Code is UndertaleCode scriptFlagSetExtCode && !flagSetExtCandidates.Contains(scriptFlagSetExtCode))
  flagSetExtCandidates.Add(scriptFlagSetExtCode);
foreach (var code in Data.Code)
{
  string codeName = code.Name.Content;
  if (codeName.Contains("scr_flag_set") && !flagSetExtCandidates.Contains(code))
    flagSetExtCandidates.Add(code);
}

foreach (var code in flagSetExtCandidates)
{
  string flagSetExtText = GetDecompiledText(code);
  if (!flagSetExtText.Contains("function scr_flag_set_ext"))
    continue;

  string flagSetExtBody = @"
function scr_flag_set_ext(arg0, arg1, arg2, arg3 = 1)
{
    return scr_tenna_flag_set_bitmask(arg0, arg1, arg2, arg3);
}
";
  string updatedFlagSetExtText = ReplaceNamedFunction(flagSetExtText, "scr_flag_set_ext", flagSetExtBody);
  if (updatedFlagSetExtText != flagSetExtText)
  {
    importGroup.QueueReplace(code, updatedFlagSetExtText);
    flagSetExtUpdated = true;
  }
}

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
  


  // Hook fresh flag writes and known old watcher wrapper forms.
  int hookedCount = 0;
  int targetedRewriteCount = 0;
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

    bool hasFreshFlagWrite = WritesFlag(code);
    bool hasKnownRewriteSurface = false;
    string originalText = "";

    if (!hasFreshFlagWrite && HasKnownFlagWatcherRewriteSurface(code))
    {
      originalText = GetDecompiledText(code);
      hasKnownRewriteSurface =
        originalText.Contains("scr_set_bitmask_value")
        || originalText.Contains("scr_flag_set_ext")
        || (originalText.Contains("global.flag") && originalText.Contains("scr_array_to_bitmask"));
      if (hasKnownRewriteSurface)
        targetedRewriteCount++;
    }

    if (!hasFreshFlagWrite && !hasKnownRewriteSurface)
      continue;

    if (originalText == "")
      originalText = GetDecompiledText(code);

    bool shouldRewrite =
      originalText.Contains("global.flag")
      || originalText.Contains("scr_set_bitmask_value")
      || originalText.Contains("scr_flag_set_ext");

    if (shouldRewrite)
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
    if (flagSetExtUpdated)
      msg += "\nBitmask flag helper updated.";
    else
      msg += "\nWarning: scr_flag_set_ext was not found; wrapper bitmask writes may display as raw parent flags.";
    if (targetedRewriteCount > 0)
      msg += "\nKnown rewrite surfaces checked: " + targetedRewriteCount + ".";
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
  codeText = HookExistingBitmaskFlagSetCalls(codeText);
  codeText = HookFlagSetExtCalls(codeText);
  Dictionary<string, string[]> arrayBitmaskLocals = FindArrayBitmaskLocals(codeText);

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
      replacement = BuildPackedSetReplacement(indexExpr, valueExpr, arrayBitmaskLocals);
      if (replacement == "")
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

string HookFlagSetExtCalls(string codeText)
{
  string callName = "scr_flag_set_ext";
  int index = 0;
  while (true)
  {
    index = FindNextCodeText(codeText, callName + "(", index);
    if (index < 0)
      break;

    int prefixStart = Math.Max(0, index - "function ".Length);
    string prefix = codeText.Substring(prefixStart, index - prefixStart);
    if (prefix == "function ")
    {
      index += callName.Length;
      continue;
    }

    int openParen = codeText.IndexOf('(', index + callName.Length);
    if (openParen < 0)
      break;

    int closeParen = FindMatchingParen(codeText, openParen);
    if (closeParen < 0)
    {
      index += callName.Length;
      continue;
    }

    List<string> args = SplitTopLevelArguments(codeText.Substring(openParen + 1, closeParen - openParen - 1));
    if (args.Count < 3 || args.Count > 4)
    {
      index = closeParen + 1;
      continue;
    }

    string widthExpr = args.Count >= 4 ? args[3].Trim() : "1";
    string replacement = $"scr_tenna_flag_set_bitmask({args[0].Trim()}, {args[1].Trim()}, {args[2].Trim()}, {widthExpr})";
    int originalLength = closeParen + 1 - index;
    codeText = codeText.Substring(0, index) + replacement + codeText.Substring(index + originalLength);
    index += replacement.Length;
  }

  return codeText;
}

string HookExistingBitmaskFlagSetCalls(string codeText)
{
  Dictionary<string, string[]> arrayBitmaskLocals = FindArrayBitmaskLocals(codeText);
  string callName = "scr_tenna_flag_set";
  int index = 0;
  while (true)
  {
    index = FindNextCodeText(codeText, callName + "(", index);
    if (index < 0)
      break;

    int openParen = codeText.IndexOf('(', index + callName.Length);
    if (openParen < 0)
      break;

    int closeParen = FindMatchingParen(codeText, openParen);
    if (closeParen < 0)
    {
      index += callName.Length;
      continue;
    }

    List<string> args = SplitTopLevelArguments(codeText.Substring(openParen + 1, closeParen - openParen - 1));
    if (args.Count != 2)
    {
      index = closeParen + 1;
      continue;
    }

    string replacement = BuildPackedSetReplacement(args[0].Trim(), args[1].Trim(), arrayBitmaskLocals);
    if (replacement == "")
    {
      index = closeParen + 1;
      continue;
    }

    int originalLength = closeParen + 1 - index;
    codeText = codeText.Substring(0, index) + replacement + codeText.Substring(index + originalLength);
    index += replacement.Length;
  }

  return codeText;
}

string BuildPackedSetReplacement(string indexExpr, string valueExpr)
{
  return BuildPackedSetReplacement(indexExpr, valueExpr, null);
}

string BuildPackedSetReplacement(string indexExpr, string valueExpr, Dictionary<string, string[]> arrayBitmaskLocals)
{
  string replacement = BuildBitmaskSetReplacement(indexExpr, valueExpr);
  if (replacement != "")
    return replacement;

  replacement = BuildArrayBitmaskSetReplacement(indexExpr, valueExpr);
  if (replacement != "")
    return replacement;

  string localName = valueExpr.Trim();
  if (arrayBitmaskLocals != null && IsIdentifier(localName) && arrayBitmaskLocals.TryGetValue(localName, out string[] localArgs))
    return $"scr_tenna_flag_set_bitmask_array({indexExpr}, {localArgs[0]}, {localArgs[1]})";

  return "";
}

string BuildBitmaskSetReplacement(string indexExpr, string valueExpr)
{
  string callName = "scr_set_bitmask_value";
  string trimmed = valueExpr.Trim();
  if (!trimmed.StartsWith(callName + "(", StringComparison.Ordinal))
    return "";

  int openParen = trimmed.IndexOf('(');
  int closeParen = FindMatchingParen(trimmed, openParen);
  if (closeParen != trimmed.Length - 1)
    return "";

  List<string> args = SplitTopLevelArguments(trimmed.Substring(openParen + 1, closeParen - openParen - 1));
  if (args.Count < 3 || args.Count > 4)
    return "";

  string targetFlagExpr = ExtractGlobalFlagIndex(args[0]);
  if (targetFlagExpr == "")
    return "";

  string normalizedTarget = RemoveWhitespace(targetFlagExpr);
  string normalizedIndex = RemoveWhitespace(indexExpr);
  if (normalizedTarget != normalizedIndex)
    return "";

  string widthExpr = args.Count >= 4 ? args[3].Trim() : "1";
  return $"scr_tenna_flag_set_bitmask({indexExpr}, {args[1].Trim()}, {args[2].Trim()}, {widthExpr})";
}

string BuildArrayBitmaskSetReplacement(string indexExpr, string valueExpr)
{
  string callName = "scr_array_to_bitmask";
  string trimmed = valueExpr.Trim();
  if (!trimmed.StartsWith(callName + "(", StringComparison.Ordinal))
    return "";

  int openParen = trimmed.IndexOf('(');
  int closeParen = FindMatchingParen(trimmed, openParen);
  if (closeParen != trimmed.Length - 1)
    return "";

  List<string> args = SplitTopLevelArguments(trimmed.Substring(openParen + 1, closeParen - openParen - 1));
  if (args.Count < 1 || args.Count > 2)
    return "";

  string widthExpr = args.Count >= 2 ? args[1].Trim() : "1";
  return $"scr_tenna_flag_set_bitmask_array({indexExpr}, {args[0].Trim()}, {widthExpr})";
}

Dictionary<string, string[]> FindArrayBitmaskLocals(string codeText)
{
  Dictionary<string, string[]> locals = new Dictionary<string, string[]>();
  string callName = "scr_array_to_bitmask";
  int index = 0;

  while (true)
  {
    index = FindNextCodeText(codeText, callName + "(", index);
    if (index < 0)
      break;

    int openParen = codeText.IndexOf('(', index + callName.Length);
    int closeParen = openParen >= 0 ? FindMatchingParen(codeText, openParen) : -1;
    if (openParen < 0 || closeParen < 0)
    {
      index += callName.Length;
      continue;
    }

    int statementStart = index - 1;
    while (statementStart >= 0)
    {
      char c = codeText[statementStart];
      if (c == ';' || c == '\n' || c == '{' || c == '}')
      {
        statementStart++;
        break;
      }
      statementStart--;
    }
    if (statementStart < 0)
      statementStart = 0;

    string assignedName = ExtractAssignedIdentifier(codeText.Substring(statementStart, index - statementStart));
    if (assignedName == "")
    {
      index = closeParen + 1;
      continue;
    }

    List<string> args = SplitTopLevelArguments(codeText.Substring(openParen + 1, closeParen - openParen - 1));
    if (args.Count >= 1 && args.Count <= 2)
    {
      string widthExpr = args.Count >= 2 ? args[1].Trim() : "1";
      locals[assignedName] = new string[] { args[0].Trim(), widthExpr };
    }

    index = closeParen + 1;
  }

  return locals;
}

string ExtractAssignedIdentifier(string prefix)
{
  string trimmed = prefix.Trim();
  if (!trimmed.EndsWith("=", StringComparison.Ordinal))
    return "";

  trimmed = trimmed.Substring(0, trimmed.Length - 1).Trim();
  if (trimmed.StartsWith("var ", StringComparison.Ordinal))
    trimmed = trimmed.Substring(4).Trim();

  if (!IsIdentifier(trimmed))
    return "";

  return trimmed;
}

bool IsIdentifier(string value)
{
  if (value.Length == 0)
    return false;

  if (!(char.IsLetter(value[0]) || value[0] == '_'))
    return false;

  for (int i = 1; i < value.Length; i++)
  {
    if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
      return false;
  }

  return true;
}

string ExtractGlobalFlagIndex(string expr)
{
  string trimmed = expr.Trim();
  if (!trimmed.StartsWith("global.flag", StringComparison.Ordinal))
    return "";

  int openBracket = trimmed.IndexOf('[', "global.flag".Length);
  if (openBracket < 0)
    return "";

  int closeBracket = FindMatchingBracket(trimmed, openBracket);
  if (closeBracket != trimmed.Length - 1)
    return "";

  return trimmed.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();
}

List<string> SplitTopLevelArguments(string source)
{
  List<string> args = new List<string>();
  int start = 0;
  int parenCount = 0;
  int bracketCount = 0;
  int curlyCount = 0;

  for (int scan = 0; scan < source.Length; scan++)
  {
    char c = source[scan];
    if (IsStringStart(source, scan))
    {
      scan = SkipStringLiteral(source, scan) - 1;
      continue;
    }

    if (c == '(') parenCount++;
    else if (c == ')') parenCount--;
    else if (c == '[') bracketCount++;
    else if (c == ']') bracketCount--;
    else if (c == '{') curlyCount++;
    else if (c == '}') curlyCount--;
    else if (c == ',' && parenCount == 0 && bracketCount == 0 && curlyCount == 0)
    {
      args.Add(source.Substring(start, scan - start).Trim());
      start = scan + 1;
    }
  }

  args.Add(source.Substring(start).Trim());
  return args;
}

int FindMatchingParen(string source, int openParen)
{
  int level = 1;
  int scan = openParen + 1;
  while (scan < source.Length && level > 0)
  {
    char c = source[scan];
    if (IsStringStart(source, scan))
    {
      scan = SkipStringLiteral(source, scan);
      continue;
    }

    if (c == '(')
      level++;
    else if (c == ')')
      level--;

    if (level > 0)
      scan++;
  }

  return level == 0 ? scan : -1;
}

int FindMatchingBracket(string source, int openBracket)
{
  int level = 1;
  int scan = openBracket + 1;
  while (scan < source.Length && level > 0)
  {
    char c = source[scan];
    if (IsStringStart(source, scan))
    {
      scan = SkipStringLiteral(source, scan);
      continue;
    }

    if (c == '[')
      level++;
    else if (c == ']')
      level--;

    if (level > 0)
      scan++;
  }

  return level == 0 ? scan : -1;
}

string RemoveWhitespace(string value)
{
  System.Text.StringBuilder builder = new System.Text.StringBuilder();
  foreach (char c in value)
  {
    if (!char.IsWhiteSpace(c))
      builder.Append(c);
  }
  return builder.ToString();
}

int FindNextCodeText(string source, string needle, int startIndex)
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

    if (scan + needle.Length <= source.Length && source.Substring(scan, needle.Length) == needle)
      return scan;

    scan++;
  }

  return -1;
}

string ReplaceNamedFunction(string source, string functionName, string replacement)
{
  string pattern = "function " + functionName;
  int functionIndex = source.IndexOf(pattern, StringComparison.Ordinal);
  if (functionIndex < 0)
    return source;

  int openBrace = source.IndexOf("{", functionIndex, StringComparison.Ordinal);
  if (openBrace < 0)
    return source;

  int level = 1;
  int scan = openBrace + 1;
  while (scan < source.Length && level > 0)
  {
    char c = source[scan];
    if (IsStringStart(source, scan))
    {
      scan = SkipStringLiteral(source, scan);
      continue;
    }

    if (c == '{')
      level++;
    else if (c == '}')
      level--;

    scan++;
  }

  if (level != 0)
    return source;

  return source.Substring(0, functionIndex) + replacement + source.Substring(scan);
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

bool WritesFlag(UndertaleCode code)
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

bool HasKnownFlagWatcherRewriteSurface(UndertaleCode code)
{
  return ReferencesInstructionText(code, "scr_flag_set_ext")
    || ReferencesInstructionText(code, "scr_set_bitmask_value")
    || ReferencesInstructionText(code, "scr_array_to_bitmask");
}

bool ReferencesInstructionText(UndertaleCode code, string needle)
{
  if (code.Instructions == null)
    return false;

  foreach (var instr in code.Instructions)
  {
    if ((instr.ToString() ?? "").Contains(needle))
      return true;

    foreach (var prop in instr.GetType().GetProperties())
    {
      object propValue;
      try
      {
        propValue = prop.GetValue(instr);
      }
      catch (Exception)
      {
        continue;
      }

      if (InstructionValueContains(propValue, needle))
        return true;
    }

    foreach (var field in instr.GetType().GetFields())
    {
      object fieldValue;
      try
      {
        fieldValue = field.GetValue(instr);
      }
      catch (Exception)
      {
        continue;
      }

      if (InstructionValueContains(fieldValue, needle))
        return true;
    }
  }

  return false;
}

bool InstructionValueContains(object value, string needle)
{
  if (value == null)
    return false;

  string text = value.ToString() ?? "";
  return text.Contains(needle);
}
