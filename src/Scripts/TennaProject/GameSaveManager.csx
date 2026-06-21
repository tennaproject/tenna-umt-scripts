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

bool saveManagerAlreadyInstalled = checkCreate.Contains("_tenna_sm_enabled");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
global._tenna_sm_enabled = true;
global._tenna_sm_open = false;
global._tenna_sm_saves[0] = """";
global._tenna_sm_count = 0;
global._tenna_sm_selected = 0;
global._tenna_sm_scroll = 0;
global._tenna_sm_mode = 0;
global._tenna_sm_input = """";
global._tenna_sm_input_active = false;
global._tenna_sm_max_visible = 12;
global._tenna_sm_msg = """";
global._tenna_sm_msg_timer = 0;

directory_create(""tenna"");
directory_create(""tenna/saves"");

scr_tenna_sm_refresh();
";

string stepCheck = @"
if (keyboard_check_pressed(ord(""S"")) && keyboard_check(vk_alt) && !global._tenna_sm_input_active)
{
    global._tenna_sm_open = !global._tenna_sm_open;
    if (global._tenna_sm_open)
    {
        global._tenna_sm_mode = 0;
        global._tenna_sm_input = """";
        global._tenna_sm_input_active = false;
        scr_tenna_sm_refresh();
        instance_deactivate_all(true);
        instance_activate_object(obj_time);
        instance_activate_object(obj_gamecontroller);
    }
    else
    {
        instance_activate_all();
    }
}

if (global._tenna_sm_open)
{
    if (global._tenna_sm_input_active)
    {
        if (keyboard_check_pressed(vk_escape) || keyboard_check_pressed(ord(""X"")))
        {
            global._tenna_sm_input_active = false;
            global._tenna_sm_input = """";
        }
        else if (keyboard_check_pressed(vk_enter))
        {
            if (string_length(global._tenna_sm_input) > 0)
            {
                scr_tenna_sm_save(global._tenna_sm_input);
                global._tenna_sm_input_active = false;
                global._tenna_sm_input = """";
                scr_tenna_sm_refresh();
            }
        }
        else if (keyboard_check_pressed(vk_backspace))
        {
            if (string_length(global._tenna_sm_input) > 0)
                global._tenna_sm_input = string_copy(global._tenna_sm_input, 1, string_length(global._tenna_sm_input) - 1);
        }
        else
        {
            var _k = keyboard_lastchar;
            if (_k != """")
            {
                var _c = ord(_k);
                if ((_c >= 48 && _c <= 57) || (_c >= 65 && _c <= 90) || (_c >= 97 && _c <= 122) || _c == 45 || _c == 95)
                    global._tenna_sm_input += _k;
            }
            keyboard_lastchar = """";
        }
    }
    else
    {
        if (keyboard_check_pressed(vk_escape))
        {
            instance_activate_all();
            global._tenna_sm_open = false;
        }
        
        if (keyboard_check_pressed(vk_up))
        {
            global._tenna_sm_selected -= 1;
            if (global._tenna_sm_selected < 0)
                global._tenna_sm_selected = global._tenna_sm_count + 1;
            
            if (global._tenna_sm_selected < global._tenna_sm_scroll)
                global._tenna_sm_scroll = global._tenna_sm_selected;
            if (global._tenna_sm_selected >= global._tenna_sm_scroll + global._tenna_sm_max_visible)
                global._tenna_sm_scroll = global._tenna_sm_selected - global._tenna_sm_max_visible + 1;
        }
        
        if (keyboard_check_pressed(vk_down))
        {
            global._tenna_sm_selected += 1;
            if (global._tenna_sm_selected > global._tenna_sm_count + 1)
                global._tenna_sm_selected = 0;
            
            if (global._tenna_sm_selected < global._tenna_sm_scroll)
                global._tenna_sm_scroll = global._tenna_sm_selected;
            if (global._tenna_sm_selected >= global._tenna_sm_scroll + global._tenna_sm_max_visible)
                global._tenna_sm_scroll = global._tenna_sm_selected - global._tenna_sm_max_visible + 1;
        }
        
        if (keyboard_check_pressed(vk_enter) || keyboard_check_pressed(ord(""Z"")))
        {
            if (global._tenna_sm_selected == 0)
            {
                global._tenna_sm_input_active = true;
                global._tenna_sm_input = """";
                keyboard_lastchar = """";
            }
            else if (global._tenna_sm_selected == 1)
            {
                var _ts = string(current_year) + ""-"" 
                    + string_format(current_month, 2, 0) + ""-"" 
                    + string_format(current_day, 2, 0) + ""_"" 
                    + string_format(current_hour, 2, 0) + ""-"" 
                    + string_format(current_minute, 2, 0) + ""-"" 
                    + string_format(current_second, 2, 0);
                _ts = string_replace_all(_ts, "" "", ""0"");
                var _qname = ""ch"" + string(global.chapter) + ""_"" + _ts;
                scr_tenna_sm_save(_qname);
                scr_tenna_sm_refresh();
            }
            else if (global._tenna_sm_selected <= global._tenna_sm_count + 1 && global._tenna_sm_mode == 0)
            {
                global._tenna_sm_mode = 1;
            }
            else if (global._tenna_sm_mode == 1)
            {
                var _slot = global._tenna_sm_saves[global._tenna_sm_selected - 2];
                instance_activate_all();
                scr_tenna_sm_load(_slot);
                global._tenna_sm_open = false;
            }
            else if (global._tenna_sm_mode == 2)
            {
                var _slot = global._tenna_sm_saves[global._tenna_sm_selected - 2];
                scr_tenna_sm_delete(_slot);
                scr_tenna_sm_refresh();
                global._tenna_sm_mode = 0;
                if (global._tenna_sm_selected > global._tenna_sm_count + 1)
                    global._tenna_sm_selected = global._tenna_sm_count + 1;
            }
        }
        
        if (keyboard_check_pressed(vk_left) || keyboard_check_pressed(vk_right))
        {
            if (global._tenna_sm_selected > 1 && global._tenna_sm_selected <= global._tenna_sm_count + 1)
            {
                if (global._tenna_sm_mode == 1)
                    global._tenna_sm_mode = 2;
                else if (global._tenna_sm_mode == 2)
                    global._tenna_sm_mode = 1;
            }
        }
        
        if (keyboard_check_pressed(ord(""X"")))
        {
            if (global._tenna_sm_mode > 0)
                global._tenna_sm_mode = 0;
            else
            {
                instance_activate_all();
                global._tenna_sm_open = false;
            }
        }
    }
}

if (global._tenna_sm_msg_timer > 0)
    global._tenna_sm_msg_timer -= 1;
";

string drawDisplay = @"
if (global._tenna_sm_open)
{
    draw_set_alpha(0.85);
    draw_set_color(c_black);
    draw_rectangle(0, 0, 640, 480, false);
    draw_set_alpha(1);
    
    draw_set_font(fnt_main);
    draw_set_halign(fa_center);
    draw_set_color(c_white);
    draw_text(320, 20, ""TENNA SAVE MANAGER"");
    
    draw_set_halign(fa_left);
    var _ystart = 70;
    var _itemh = 22;
    
    var _new_color = (global._tenna_sm_selected == 0) ? c_yellow : c_lime;
    draw_set_color(_new_color);
    if (global._tenna_sm_input_active)
    {
        var _blink = (current_time div 500) mod 2;
        var _cursor = (_blink == 0) ? ""_"" : """";
        draw_text(40, _ystart, ""> File Name: "" + global._tenna_sm_input + _cursor);
    }
    else
    {
        draw_text(40, _ystart, (global._tenna_sm_selected == 0 ? ""> "" : ""  "") + ""New Save"");
    }
    
    var _quick_color = (global._tenna_sm_selected == 1) ? c_yellow : c_aqua;
    draw_set_color(_quick_color);
    draw_text(40, _ystart + _itemh, (global._tenna_sm_selected == 1 ? ""> "" : ""  "") + ""Quick Save"");
    
    draw_set_color(c_dkgray);
    draw_line(40, _ystart + _itemh * 2, 600, _ystart + _itemh * 2);
    
    for (var _i = 0; _i < global._tenna_sm_max_visible; _i++)
    {
        var _idx = global._tenna_sm_scroll + _i;
        if (_idx >= global._tenna_sm_count)
            break;
        
        var _ypos = _ystart + _itemh * 2 + 8 + (_i * _itemh);
        var _sel = (_idx + 2 == global._tenna_sm_selected);
        
        if (_sel)
        {
            draw_set_color(c_yellow);
            draw_text(40, _ypos, ""> "" + global._tenna_sm_saves[_idx]);
            
            if (global._tenna_sm_mode > 0)
            {
                var _lc = (global._tenna_sm_mode == 1) ? c_lime : c_gray;
                var _dc = (global._tenna_sm_mode == 2) ? c_red : c_gray;
                draw_set_color(_lc);
                draw_text(400, _ypos, ""Load"");
                draw_set_color(_dc);
                draw_text(480, _ypos, ""Delete"");
            }
        }
        else
        {
            draw_set_color(c_white);
            draw_text(40, _ypos, ""  "" + global._tenna_sm_saves[_idx]);
        }
    }
    
    if (global._tenna_sm_count > global._tenna_sm_max_visible)
    {
        draw_set_halign(fa_right);
        draw_set_color(c_gray);
        var _scrollinfo = string(global._tenna_sm_scroll + 1) + ""-"" + string(min(global._tenna_sm_scroll + global._tenna_sm_max_visible, global._tenna_sm_count)) + "" / "" + string(global._tenna_sm_count);
        draw_text(600, _ystart + _itemh + 8 + (global._tenna_sm_max_visible * _itemh) + 10, _scrollinfo);
    }
    
    draw_set_halign(fa_center);
    draw_set_color(c_gray);
    draw_text(320, 440, ""[Arrow Keys] Navigate  |  [Z/Enter] Select  |  [X/Esc] Back"");
    draw_text(320, 458, ""[Left/Right] Switch Action"");
    
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
        draw_set_valign(fa_top);
    }
    
    draw_set_halign(fa_left);
    draw_set_color(c_white);
}

if (global._tenna_sm_msg_timer > 0)
{
    draw_set_font(fnt_main);
    draw_set_halign(fa_center);
    draw_set_alpha(min(1, global._tenna_sm_msg_timer / 30));
    draw_set_color(c_black);
    draw_text(321, 421, global._tenna_sm_msg);
    draw_set_color(c_lime);
    draw_text(320, 420, global._tenna_sm_msg);
    draw_set_alpha(1);
    draw_set_halign(fa_left);
    draw_set_color(c_white);
}
";

var saveDirFunctionName = "scr_tenna_sm_save_dir";
UndertaleCode saveDirCode;
if (Data.Scripts.ByName(saveDirFunctionName)?.Code is UndertaleCode existingSaveDirCode)
{
  saveDirCode = existingSaveDirCode;
}
else
{
  saveDirCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + saveDirFunctionName) };
  Data.Code.Add(saveDirCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(saveDirFunctionName), Code = saveDirCode };
  Data.Scripts.Add(scriptEntry);
}

string saveDirBody = @"
directory_create(""tenna"");
directory_create(""tenna/saves"");

var _chapter = 0;
if (variable_global_exists(""chapter""))
    _chapter = global.chapter;

var _dir = ""tenna/saves/chapter"" + string(_chapter);
directory_create(_dir);
return _dir;
";
importGroup.QueueReplace(saveDirCode, saveDirBody);

var refreshFunctionName = "scr_tenna_sm_refresh";
UndertaleCode refreshCode;
if (Data.Scripts.ByName(refreshFunctionName)?.Code is UndertaleCode existingRefreshCode)
{
  refreshCode = existingRefreshCode;
}
else
{
  refreshCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + refreshFunctionName) };
  Data.Code.Add(refreshCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(refreshFunctionName), Code = refreshCode };
  Data.Scripts.Add(scriptEntry);
}

string refreshBody = @"
global._tenna_sm_count = 0;
global._tenna_sm_saves[0] = """";

var _save_dir = scr_tenna_sm_save_dir();
var _search = file_find_first(_save_dir + ""/*"", 0);
while (_search != """")
{
    if (string_pos(""."", _search) == 0 && _search != """")
    {
        global._tenna_sm_saves[global._tenna_sm_count] = _search;
        global._tenna_sm_count += 1;
    }
    _search = file_find_next();
}
file_find_close();

for (var _i = 0; _i < global._tenna_sm_count - 1; _i++)
{
    for (var _j = _i + 1; _j < global._tenna_sm_count; _j++)
    {
        if (global._tenna_sm_saves[_i] > global._tenna_sm_saves[_j])
        {
            var _tmp = global._tenna_sm_saves[_i];
            global._tenna_sm_saves[_i] = global._tenna_sm_saves[_j];
            global._tenna_sm_saves[_j] = _tmp;
        }
    }
}
";
importGroup.QueueReplace(refreshCode, refreshBody);

var writeRealFunctionName = "scr_tenna_sm_write_real";
UndertaleCode writeRealCode;
if (Data.Scripts.ByName(writeRealFunctionName)?.Code is UndertaleCode existingWriteRealCode)
{
  writeRealCode = existingWriteRealCode;
}
else
{
  writeRealCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + writeRealFunctionName) };
  Data.Code.Add(writeRealCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(writeRealFunctionName), Code = writeRealCode };
  Data.Scripts.Add(scriptEntry);
}

string writeRealBody = @"
var _file = argument0;
var _value = argument1;

if (is_string(_value))
{
    var _ok = string_length(_value) > 0;
    for (var _i = 1; _i <= string_length(_value); _i++)
    {
        var _c = ord(string_char_at(_value, _i));
        if (_i == 1 && _c == 45)
            continue;
        if (_c < 48 || _c > 57)
        {
            _ok = false;
            break;
        }
    }

    if (_ok)
        file_text_write_real(_file, real(_value));
    else
        file_text_write_real(_file, 0);
}
else
{
    file_text_write_real(_file, _value);
}
";
importGroup.QueueReplace(writeRealCode, writeRealBody);

var getGlobal2dFunctionName = "scr_tenna_sm_get_global_2d";
UndertaleCode getGlobal2dCode;
if (Data.Scripts.ByName(getGlobal2dFunctionName)?.Code is UndertaleCode existingGetGlobal2dCode)
{
  getGlobal2dCode = existingGetGlobal2dCode;
}
else
{
  getGlobal2dCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + getGlobal2dFunctionName) };
  Data.Code.Add(getGlobal2dCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(getGlobal2dFunctionName), Code = getGlobal2dCode };
  Data.Scripts.Add(scriptEntry);
}

string getGlobal2dBody = @"
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
importGroup.QueueReplace(getGlobal2dCode, getGlobal2dBody);

var setGlobal2dFunctionName = "scr_tenna_sm_set_global_2d";
UndertaleCode setGlobal2dCode;
if (Data.Scripts.ByName(setGlobal2dFunctionName)?.Code is UndertaleCode existingSetGlobal2dCode)
{
  setGlobal2dCode = existingSetGlobal2dCode;
}
else
{
  setGlobal2dCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + setGlobal2dFunctionName) };
  Data.Code.Add(setGlobal2dCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(setGlobal2dFunctionName), Code = setGlobal2dCode };
  Data.Scripts.Add(scriptEntry);
}

string setGlobal2dBody = @"
var _name = argument0;
var _y = argument1;
var _x = argument2;
var _value = argument3;

if (!variable_global_exists(_name))
    return;

var _array = variable_global_get(_name);
if (_y < 0 || _x < 0)
    return;
if (!is_array(_array))
    return;
if (_y >= array_height_2d(_array))
    return;
if (_x >= array_length_2d(_array, _y))
    return;

_array[_y][_x] = _value;
";
importGroup.QueueReplace(setGlobal2dCode, setGlobal2dBody);

var saveFunctionName = "scr_tenna_sm_save";
UndertaleCode saveCode;
if (Data.Scripts.ByName(saveFunctionName)?.Code is UndertaleCode existingSaveCode)
{
  saveCode = existingSaveCode;
}
else
{
  saveCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + saveFunctionName) };
  Data.Code.Add(saveCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(saveFunctionName), Code = saveCode };
  Data.Scripts.Add(scriptEntry);
}

string saveBody = @"
var _name = argument0;
var _safe_name = """";
for (var _i = 1; _i <= string_length(_name); _i++)
{
    var _char = string_char_at(_name, _i);
    var _code = ord(_char);
    if ((_code >= 48 && _code <= 57) || (_code >= 65 && _code <= 90) || (_code >= 97 && _code <= 122) || _code == 45 || _code == 95)
        _safe_name += _char;
    else if (_char == "" "")
        _safe_name += ""_"";
}

if (_safe_name == """")
{
    global._tenna_sm_msg = ""Invalid save name!"";
    global._tenna_sm_msg_timer = 120;
    return;
}

_name = _safe_name;
directory_create(""tenna"");
directory_create(""tenna/saves"");
var _file = scr_tenna_sm_save_dir() + ""/"" + _name;

var _f = file_text_open_write(_file);
var _is_ch1 = variable_global_exists(""chapter"") && global.chapter == 1;

file_text_write_string(_f, global.truename);
file_text_writeln(_f);

for (var _i = 0; _i < 6; _i++)
{
    file_text_write_string(_f, global.othername[_i]);
    file_text_writeln(_f);
}

scr_tenna_sm_write_real(_f, global.char[0]);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.char[1]);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.char[2]);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.gold);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.xp);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.lv);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.inv);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.invc);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.darkzone);
file_text_writeln(_f);

var _party_count = _is_ch1 ? 4 : 5;
for (var _i = 0; _i < _party_count; _i++)
{
    scr_tenna_sm_write_real(_f, global.hp[_i]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.maxhp[_i]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.at[_i]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.df[_i]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.mag[_i]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.guts[_i]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.charweapon[_i]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.chararmor1[_i]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.chararmor2[_i]);
    file_text_writeln(_f);
    if (_is_ch1)
        file_text_write_string(_f, string(global.weaponstyle[_i]));
    else
        scr_tenna_sm_write_real(_f, global.weaponstyle[_i]);
    file_text_writeln(_f);
    
    for (var _q = 0; _q < 4; _q++)
    {
        scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itemat"", _i, _q));
        file_text_writeln(_f);
        scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itemdf"", _i, _q));
        file_text_writeln(_f);
        scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itemmag"", _i, _q));
        file_text_writeln(_f);
        scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itembolts"", _i, _q));
        file_text_writeln(_f);
        scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itemgrazeamt"", _i, _q));
        file_text_writeln(_f);
        scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itemgrazesize"", _i, _q));
        file_text_writeln(_f);
        scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itemboltspeed"", _i, _q));
        file_text_writeln(_f);
        scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itemspecial"", _i, _q));
        file_text_writeln(_f);
        if (!_is_ch1)
        {
            scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itemelement"", _i, _q));
            file_text_writeln(_f);
            scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""itemelementamount"", _i, _q));
            file_text_writeln(_f);
        }
    }
    
    for (var _j = 0; _j < 12; _j++)
    {
        scr_tenna_sm_write_real(_f, scr_tenna_sm_get_global_2d(""spell"", _i, _j));
        file_text_writeln(_f);
    }
}

scr_tenna_sm_write_real(_f, global.boltspeed);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.grazeamt);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.grazesize);
file_text_writeln(_f);

for (var _j = 0; _j < 13; _j++)
{
    scr_tenna_sm_write_real(_f, global.item[_j]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.keyitem[_j]);
    file_text_writeln(_f);
}

var _equip_count = _is_ch1 ? 13 : 48;
for (var _j = 0; _j < _equip_count; _j++)
{
    scr_tenna_sm_write_real(_f, global.weapon[_j]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.armor[_j]);
    file_text_writeln(_f);
}

if (!_is_ch1)
{
    for (var _j = 0; _j < 72; _j++)
    {
        scr_tenna_sm_write_real(_f, global.pocketitem[_j]);
        file_text_writeln(_f);
    }
}

scr_tenna_sm_write_real(_f, global.tension);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.maxtension);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.lweapon);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.larmor);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.lxp);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.llv);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.lgold);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.lhp);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.lmaxhp);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.lat);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.ldf);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.lwstrength);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.ladef);
file_text_writeln(_f);

for (var _i = 0; _i < 8; _i++)
{
    scr_tenna_sm_write_real(_f, global.litem[_i]);
    file_text_writeln(_f);
    scr_tenna_sm_write_real(_f, global.phone[_i]);
    file_text_writeln(_f);
}

var _flag_count = _is_ch1 ? 9999 : 2500;
for (var _i = 0; _i < _flag_count; _i++)
{
    scr_tenna_sm_write_real(_f, global.flag[_i]);
    file_text_writeln(_f);
}

scr_tenna_sm_write_real(_f, global.plot);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.currentroom);
file_text_writeln(_f);
scr_tenna_sm_write_real(_f, global.time);
file_text_writeln(_f);

file_text_close(_f);

global._tenna_sm_msg = ""Saved: "" + _name;
global._tenna_sm_msg_timer = 120;
scr_tenna_log(""SaveManager"", ""Saved to slot: "" + _name);
";
importGroup.QueueReplace(saveCode, saveBody);

var loadFunctionName = "scr_tenna_sm_load";
UndertaleCode loadCode;
if (Data.Scripts.ByName(loadFunctionName)?.Code is UndertaleCode existingLoadCode)
{
  loadCode = existingLoadCode;
}
else
{
  loadCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + loadFunctionName) };
  Data.Code.Add(loadCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(loadFunctionName), Code = loadCode };
  Data.Scripts.Add(scriptEntry);
}

string loadBody = @"
var _name = argument0;
var _file = scr_tenna_sm_save_dir() + ""/"" + _name;

if (!file_exists(_file))
{
    global._tenna_sm_msg = ""File not found!"";
    global._tenna_sm_msg_timer = 120;
    return;
}

snd_free_all();
scr_gamestart();

var _f = file_text_open_read(_file);
var _is_ch1 = variable_global_exists(""chapter"") && global.chapter == 1;

global.truename = file_text_read_string(_f);
file_text_readln(_f);

for (var _i = 0; _i < 6; _i++)
{
    global.othername[_i] = file_text_read_string(_f);
    file_text_readln(_f);
}

global.char[0] = file_text_read_real(_f);
file_text_readln(_f);
global.char[1] = file_text_read_real(_f);
file_text_readln(_f);
global.char[2] = file_text_read_real(_f);
file_text_readln(_f);
global.gold = file_text_read_real(_f);
file_text_readln(_f);
global.xp = file_text_read_real(_f);
file_text_readln(_f);
global.lv = file_text_read_real(_f);
file_text_readln(_f);
global.inv = file_text_read_real(_f);
file_text_readln(_f);
global.invc = file_text_read_real(_f);
file_text_readln(_f);
global.darkzone = file_text_read_real(_f);
file_text_readln(_f);

var _party_count = _is_ch1 ? 4 : 5;
for (var _i = 0; _i < _party_count; _i++)
{
    global.hp[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    global.maxhp[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    global.at[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    global.df[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    global.mag[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    global.guts[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    global.charweapon[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    global.chararmor1[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    global.chararmor2[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    if (_is_ch1)
        global.weaponstyle[_i] = file_text_read_string(_f);
    else
        global.weaponstyle[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    
    for (var _q = 0; _q < 4; _q++)
    {
        scr_tenna_sm_set_global_2d(""itemat"", _i, _q, file_text_read_real(_f));
        file_text_readln(_f);
        scr_tenna_sm_set_global_2d(""itemdf"", _i, _q, file_text_read_real(_f));
        file_text_readln(_f);
        scr_tenna_sm_set_global_2d(""itemmag"", _i, _q, file_text_read_real(_f));
        file_text_readln(_f);
        scr_tenna_sm_set_global_2d(""itembolts"", _i, _q, file_text_read_real(_f));
        file_text_readln(_f);
        scr_tenna_sm_set_global_2d(""itemgrazeamt"", _i, _q, file_text_read_real(_f));
        file_text_readln(_f);
        scr_tenna_sm_set_global_2d(""itemgrazesize"", _i, _q, file_text_read_real(_f));
        file_text_readln(_f);
        scr_tenna_sm_set_global_2d(""itemboltspeed"", _i, _q, file_text_read_real(_f));
        file_text_readln(_f);
        scr_tenna_sm_set_global_2d(""itemspecial"", _i, _q, file_text_read_real(_f));
        file_text_readln(_f);
        if (!_is_ch1)
        {
            scr_tenna_sm_set_global_2d(""itemelement"", _i, _q, file_text_read_real(_f));
            file_text_readln(_f);
            scr_tenna_sm_set_global_2d(""itemelementamount"", _i, _q, file_text_read_real(_f));
            file_text_readln(_f);
        }
    }
    
    for (var _j = 0; _j < 12; _j++)
    {
        scr_tenna_sm_set_global_2d(""spell"", _i, _j, file_text_read_real(_f));
        file_text_readln(_f);
    }
}

global.boltspeed = file_text_read_real(_f);
file_text_readln(_f);
global.grazeamt = file_text_read_real(_f);
file_text_readln(_f);
global.grazesize = file_text_read_real(_f);
file_text_readln(_f);

for (var _j = 0; _j < 13; _j++)
{
    global.item[_j] = file_text_read_real(_f);
    file_text_readln(_f);
    global.keyitem[_j] = file_text_read_real(_f);
    file_text_readln(_f);
}

var _equip_count = _is_ch1 ? 13 : 48;
for (var _j = 0; _j < _equip_count; _j++)
{
    global.weapon[_j] = file_text_read_real(_f);
    file_text_readln(_f);
    global.armor[_j] = file_text_read_real(_f);
    file_text_readln(_f);
}

if (!_is_ch1)
{
    for (var _j = 0; _j < 72; _j++)
    {
        global.pocketitem[_j] = file_text_read_real(_f);
        file_text_readln(_f);
    }
}

global.tension = file_text_read_real(_f);
file_text_readln(_f);
global.maxtension = file_text_read_real(_f);
file_text_readln(_f);
global.lweapon = file_text_read_real(_f);
file_text_readln(_f);
global.larmor = file_text_read_real(_f);
file_text_readln(_f);
global.lxp = file_text_read_real(_f);
file_text_readln(_f);
global.llv = file_text_read_real(_f);
file_text_readln(_f);
global.lgold = file_text_read_real(_f);
file_text_readln(_f);
global.lhp = file_text_read_real(_f);
file_text_readln(_f);
global.lmaxhp = file_text_read_real(_f);
file_text_readln(_f);
global.lat = file_text_read_real(_f);
file_text_readln(_f);
global.ldf = file_text_read_real(_f);
file_text_readln(_f);
global.lwstrength = file_text_read_real(_f);
file_text_readln(_f);
global.ladef = file_text_read_real(_f);
file_text_readln(_f);

for (var _i = 0; _i < 8; _i++)
{
    global.litem[_i] = file_text_read_real(_f);
    file_text_readln(_f);
    global.phone[_i] = file_text_read_real(_f);
    file_text_readln(_f);
}

var _flag_count = _is_ch1 ? 9999 : 2500;
for (var _i = 0; _i < _flag_count; _i++)
{
    global.flag[_i] = file_text_read_real(_f);
    file_text_readln(_f);
}

global.plot = file_text_read_real(_f);
file_text_readln(_f);
global.currentroom = file_text_read_real(_f);
file_text_readln(_f);
global.time = file_text_read_real(_f);
file_text_readln(_f);

file_text_close(_f);

global.lastsavedtime = global.time;
global.lastsavedlv = global.lv;

audio_group_set_gain(1, global.flag[15], 0);
audio_set_master_gain(0, global.flag[17]);

var _room_id = global.currentroom;
if (_room_id < 10000)
{
    _room_id = scr_get_id_by_room_index(global.currentroom);
    if (_room_id == room_gms_debug_failsafe)
        _room_id += (global.chapter * 10000);
    global.currentroom = _room_id;
}

var _loadedroom = scr_get_room_by_id(global.currentroom);

if (scr_dogcheck())
    _loadedroom = 92;

scr_tempsave();

with (obj_gamecontroller)
    enable_loading();

global._tenna_sm_msg = ""Loaded: "" + _name;
global._tenna_sm_msg_timer = 120;
scr_tenna_log(""SaveManager"", ""Loaded from slot: "" + _name);

room_goto(_loadedroom);
";
importGroup.QueueReplace(loadCode, loadBody);

var deleteFunctionName = "scr_tenna_sm_delete";
UndertaleCode deleteCode;
if (Data.Scripts.ByName(deleteFunctionName)?.Code is UndertaleCode existingDeleteCode)
{
  deleteCode = existingDeleteCode;
}
else
{
  deleteCode = new UndertaleCode { Name = Data.Strings.MakeString("gml_Script_" + deleteFunctionName) };
  Data.Code.Add(deleteCode);
  var scriptEntry = new UndertaleScript { Name = Data.Strings.MakeString(deleteFunctionName), Code = deleteCode };
  Data.Scripts.Add(scriptEntry);
}

string deleteBody = @"
var _name = argument0;
var _file = scr_tenna_sm_save_dir() + ""/"" + _name;

if (file_exists(_file))
{
    file_delete(_file);
    global._tenna_sm_msg = ""Deleted: "" + _name;
    global._tenna_sm_msg_timer = 120;
    scr_tenna_log(""SaveManager"", ""Deleted slot: "" + _name);
}
";
importGroup.QueueReplace(deleteCode, deleteBody);

try
{
  if (!saveManagerAlreadyInstalled)
  {
    importGroup.QueueReplace(createCode, GetDecompiledText(createCode) + createInit);
    importGroup.QueueReplace(stepCode, GetDecompiledText(stepCode) + stepCheck);
    importGroup.QueueReplace(drawCode, GetDecompiledText(drawCode) + drawDisplay);
  }
  
  importGroup.Import();
  if (Environment.GetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES") != "1")
    ScriptMessage("Save Manager " + (saveManagerAlreadyInstalled ? "updated" : "installed") + "!\n\nAlt+S to open save menu.\n\nControls:\n- Arrow keys: Navigate\n- Z/Enter: Select\n- X/Esc: Back\n- Left/Right: Switch action");
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
}
