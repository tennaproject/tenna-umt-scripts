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
  ScriptError("Tenna Core is required!\n\nPlease install Core.csx first.");
  return;
}

if (checkCreate.Contains("_tenna_pw_enabled"))
{
  ScriptError("Plot Watcher is already installed!");
  return;
}

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
_tenna_pw_enabled = true;
_tenna_pw_visible = true;
_tenna_pw_shadow = global.plot;
_tenna_pw_notify_msg = """";
_tenna_pw_notify_timer = 0;
";

string stepCheck = @"
if (keyboard_check_pressed(ord(""3"")) && keyboard_check(vk_alt))
    _tenna_pw_visible = !_tenna_pw_visible;

if (_tenna_pw_enabled)
{
    if (global.plot != _tenna_pw_shadow)
    {
        var _tenna_pw_old = _tenna_pw_shadow;
        var _tenna_pw_new = global.plot;
        _tenna_pw_shadow = _tenna_pw_new;
        
        _tenna_pw_notify_msg = ""Plot: "" + string(_tenna_pw_old) + "" -> "" + string(_tenna_pw_new);
        _tenna_pw_notify_timer = 180;
        
        scr_tenna_log(""PlotWatcher"", string(_tenna_pw_old) + "" -> "" + string(_tenna_pw_new));
    }
    
    if (_tenna_pw_notify_timer > 0)
        _tenna_pw_notify_timer--;
}
";

string drawDisplay = @"
if (_tenna_pw_visible)
{
    draw_set_font(fnt_main);
    draw_set_halign(fa_left);
    
    var _tenna_pw_text = ""Plot: "" + string(global.plot);
    draw_set_color(c_black);
    draw_text(9, 9, _tenna_pw_text);
    draw_set_color(c_lime);
    draw_text(8, 8, _tenna_pw_text);
    
    if (_tenna_pw_notify_timer > 0)
    {
        var _tenna_pw_alpha = _tenna_pw_notify_timer / 180;
        draw_set_alpha(_tenna_pw_alpha);
        draw_set_color(c_black);
        draw_text(9, 23, _tenna_pw_notify_msg);
        draw_set_color(c_red);
        draw_text(8, 22, _tenna_pw_notify_msg);
        draw_set_alpha(1);
    }
    
    draw_set_color(c_white);
}
";

try
{
  importGroup.QueueReplace(createCode, GetDecompiledText(createCode) + createInit);
  importGroup.QueueReplace(stepCode, GetDecompiledText(stepCode) + stepCheck);
  importGroup.QueueReplace(drawCode, GetDecompiledText(drawCode) + drawDisplay);
  
  importGroup.Import();
  ScriptMessage("Plot Watcher installed!\n\nAlt+3 to toggle display.");
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
}
