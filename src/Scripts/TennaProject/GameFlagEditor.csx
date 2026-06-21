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

bool flagEditorAlreadyInstalled = checkCreate.Contains("_tenna_fe_enabled");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
global._tenna_fe_enabled = true;
global._tenna_fe_open = false;
global._tenna_fe_field = 0;
global._tenna_fe_flag_id = 0;
global._tenna_fe_value = 0;
global._tenna_fe_flag_input = ""0"";
global._tenna_fe_value_input = ""0"";
global._tenna_fe_confirm = false;
global._tenna_fe_msg = """";
global._tenna_fe_msg_timer = 0;

directory_create(""tenna"");
";

string stepCheck = @"
if (keyboard_check_pressed(ord(""4"")) && keyboard_check(vk_alt))
{
    global._tenna_fe_open = !global._tenna_fe_open;
    global._tenna_fe_confirm = false;
    keyboard_lastchar = """";
    if (global._tenna_fe_open)
    {
        instance_deactivate_all(true);
        instance_activate_object(obj_time);
        instance_activate_object(obj_gamecontroller);
    }
    else
    {
        instance_activate_all();
    }
}

if (global._tenna_fe_open)
{
    if (keyboard_check_pressed(vk_escape) || keyboard_check_pressed(ord(""X"")))
    {
        if (global._tenna_fe_confirm)
            global._tenna_fe_confirm = false;
        else
        {
            instance_activate_all();
            global._tenna_fe_open = false;
        }
    }
    else
    {
        if (keyboard_check_pressed(vk_up) || keyboard_check_pressed(vk_down))
        {
            global._tenna_fe_field = 1 - global._tenna_fe_field;
            global._tenna_fe_confirm = false;
        }
        
        if (keyboard_check_pressed(vk_left) || keyboard_check_pressed(vk_right))
        {
            var _delta = keyboard_check(vk_shift) ? 10 : 1;
            if (keyboard_check_pressed(vk_left))
                _delta = -_delta;
            
            if (global._tenna_fe_field == 0)
            {
                global._tenna_fe_flag_id += _delta;
                if (global._tenna_fe_flag_id < 0)
                    global._tenna_fe_flag_id = 0;
                if (global._tenna_fe_flag_id > 2499)
                    global._tenna_fe_flag_id = 2499;
                global._tenna_fe_flag_input = string(global._tenna_fe_flag_id);
            }
            else
            {
                global._tenna_fe_value += _delta;
                if (global._tenna_fe_value < 0)
                    global._tenna_fe_value = 0;
                global._tenna_fe_value_input = string(global._tenna_fe_value);
            }
            global._tenna_fe_confirm = false;
        }
        
        if (keyboard_check_pressed(vk_backspace))
        {
            if (global._tenna_fe_field == 0)
            {
                if (string_length(global._tenna_fe_flag_input) > 0)
                    global._tenna_fe_flag_input = string_copy(global._tenna_fe_flag_input, 1, string_length(global._tenna_fe_flag_input) - 1);
                if (global._tenna_fe_flag_input == """")
                    global._tenna_fe_flag_input = ""0"";
                global._tenna_fe_flag_id = real(global._tenna_fe_flag_input);
            }
            else
            {
                if (string_length(global._tenna_fe_value_input) > 0)
                    global._tenna_fe_value_input = string_copy(global._tenna_fe_value_input, 1, string_length(global._tenna_fe_value_input) - 1);
                if (global._tenna_fe_value_input == """")
                    global._tenna_fe_value_input = ""0"";
                global._tenna_fe_value = real(global._tenna_fe_value_input);
            }
            scr_tenna_fe_clamp();
            global._tenna_fe_confirm = false;
        }
        else
        {
            var _k = keyboard_lastchar;
            if (_k != """")
            {
                var _c = ord(_k);
                if (_c >= 48 && _c <= 57)
                {
                    if (global._tenna_fe_field == 0)
                    {
                        if (global._tenna_fe_flag_input == ""0"")
                            global._tenna_fe_flag_input = _k;
                        else if (string_length(global._tenna_fe_flag_input) < 4)
                            global._tenna_fe_flag_input += _k;
                        global._tenna_fe_flag_id = real(global._tenna_fe_flag_input);
                    }
                    else
                    {
                        if (global._tenna_fe_value_input == ""0"")
                            global._tenna_fe_value_input = _k;
                        else if (string_length(global._tenna_fe_value_input) < 8)
                            global._tenna_fe_value_input += _k;
                        global._tenna_fe_value = real(global._tenna_fe_value_input);
                    }
                    scr_tenna_fe_clamp();
                    global._tenna_fe_confirm = false;
                }
                keyboard_lastchar = """";
            }
        }
        
        if (keyboard_check_pressed(vk_enter) || keyboard_check_pressed(ord(""Z"")))
        {
            scr_tenna_fe_clamp();
            if (global._tenna_fe_confirm)
            {
                scr_tenna_fe_apply();
                global._tenna_fe_confirm = false;
            }
            else
            {
                global._tenna_fe_confirm = true;
            }
        }
    }
}

if (global._tenna_fe_msg_timer > 0)
    global._tenna_fe_msg_timer -= 1;
";

string drawDisplay = @"
if (global._tenna_fe_open)
{
    draw_set_alpha(0.85);
    draw_set_color(c_black);
    draw_rectangle(120, 110, 520, 340, false);
    draw_set_alpha(1);
    
    draw_set_font(fnt_main);
    draw_set_halign(fa_center);
    draw_set_color(c_white);
    draw_text(320, 128, ""TENNA FLAG EDITOR"");
    
    draw_set_halign(fa_left);
    var _flag_color = (global._tenna_fe_field == 0) ? c_yellow : c_white;
    draw_set_color(_flag_color);
    draw_text(170, 170, (global._tenna_fe_field == 0 ? ""> "" : ""  "") + ""Flag ID: "" + global._tenna_fe_flag_input);
    
    var _value_color = (global._tenna_fe_field == 1) ? c_yellow : c_white;
    draw_set_color(_value_color);
    draw_text(170, 205, (global._tenna_fe_field == 1 ? ""> "" : ""  "") + ""Value: "" + global._tenna_fe_value_input);
    
    draw_set_color(c_aqua);
    draw_text(170, 242, ""Current: "" + string(global.flag[global._tenna_fe_flag_id]));
    
    if (global._tenna_fe_confirm)
    {
        draw_set_color(c_lime);
        draw_text(170, 275, ""Press Enter again: Flag["" + string(global._tenna_fe_flag_id) + ""] "" + string(global.flag[global._tenna_fe_flag_id]) + "" -> "" + string(global._tenna_fe_value));
    }
    else
    {
        draw_set_color(c_gray);
        draw_text(170, 275, ""Enter previews, Enter again applies"");
    }
    
    draw_set_halign(fa_center);
    draw_set_color(c_gray);
    draw_text(320, 312, ""Up/Down field  |  Left/Right adjust  |  X/Esc close"");
    draw_set_halign(fa_left);
    draw_set_color(c_white);
}

if (global._tenna_fe_msg_timer > 0)
{
    draw_set_font(fnt_main);
    draw_set_halign(fa_center);
    draw_set_alpha(min(1, global._tenna_fe_msg_timer / 30));
    draw_set_color(c_black);
    draw_text(321, 401, global._tenna_fe_msg);
    draw_set_color(c_lime);
    draw_text(320, 400, global._tenna_fe_msg);
    draw_set_alpha(1);
    draw_set_halign(fa_left);
    draw_set_color(c_white);
}
";

var clampFuncName = "scr_tenna_fe_clamp";
UndertaleCode clampCode;
if (Data.Scripts.ByName(clampFuncName)?.Code is UndertaleCode existingClampCode)
{
  clampCode = existingClampCode;
}
else
{
  var clampCodeName = "gml_Script_" + clampFuncName;
  clampCode = new UndertaleCode { Name = Data.Strings.MakeString(clampCodeName) };
  Data.Code.Add(clampCode);
  var clampScript = new UndertaleScript { Name = Data.Strings.MakeString(clampFuncName), Code = clampCode };
  Data.Scripts.Add(clampScript);
}

string clampFuncBody = @"
if (global._tenna_fe_flag_id < 0)
    global._tenna_fe_flag_id = 0;
if (global._tenna_fe_flag_id > 2499)
    global._tenna_fe_flag_id = 2499;
global._tenna_fe_flag_id = floor(global._tenna_fe_flag_id);
global._tenna_fe_flag_input = string(global._tenna_fe_flag_id);

if (global._tenna_fe_value < 0)
    global._tenna_fe_value = 0;
global._tenna_fe_value = floor(global._tenna_fe_value);
global._tenna_fe_value_input = string(global._tenna_fe_value);
";
importGroup.QueueReplace(clampCode, clampFuncBody);

var applyFuncName = "scr_tenna_fe_apply";
UndertaleCode applyCode;
if (Data.Scripts.ByName(applyFuncName)?.Code is UndertaleCode existingApplyCode)
{
  applyCode = existingApplyCode;
}
else
{
  var applyCodeName = "gml_Script_" + applyFuncName;
  applyCode = new UndertaleCode { Name = Data.Strings.MakeString(applyCodeName) };
  Data.Code.Add(applyCode);
  var applyScript = new UndertaleScript { Name = Data.Strings.MakeString(applyFuncName), Code = applyCode };
  Data.Scripts.Add(applyScript);
}

string applyFuncBody = @"
var _flag_id = global._tenna_fe_flag_id;
var _old = global.flag[_flag_id];
var _new = global._tenna_fe_value;
global.flag[_flag_id] = _new;

global._tenna_fe_msg = ""Flag["" + string(_flag_id) + ""]: "" + string(_old) + "" -> "" + string(_new);
global._tenna_fe_msg_timer = 120;
";
importGroup.QueueReplace(applyCode, applyFuncBody);

try
{
  if (!flagEditorAlreadyInstalled)
  {
    importGroup.QueueReplace(createCode, GetDecompiledText(createCode) + createInit);
    importGroup.QueueReplace(stepCode, GetDecompiledText(stepCode) + stepCheck);
    importGroup.QueueReplace(drawCode, GetDecompiledText(drawCode) + drawDisplay);
  }
  
  importGroup.Import();
  if (Environment.GetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES") != "1")
    ScriptMessage("Flag Editor " + (flagEditorAlreadyInstalled ? "updated" : "installed") + "!\n\nAlt+4 opens the editor.");
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
}
