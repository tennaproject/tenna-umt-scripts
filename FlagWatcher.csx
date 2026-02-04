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
if (checkCreate.Contains("_tenna_fw_enabled"))
{
  ScriptError("Flag Watcher is already installed!");
  return;
}

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
_tenna_fw_max_log = 20;
_tenna_fw_enabled = true;
for (var _tenna_fw_i = 0; _tenna_fw_i < 2000; _tenna_fw_i++)
    _tenna_fw_shadow[_tenna_fw_i] = global.flag[_tenna_fw_i];

for (var _tenna_fw_i = 0; _tenna_fw_i < _tenna_fw_max_log; _tenna_fw_i++)
    _tenna_fw_log[_tenna_fw_i] = """";

_tenna_fw_log_count = 0;
_tenna_fw_start_time = current_time;
";

string stepCheck = @"
if (_tenna_fw_enabled)
{
    for (var _tenna_fw_i = 0; _tenna_fw_i < 2000; _tenna_fw_i++)
    {
        if (_tenna_fw_i == 33)
            continue;
        
        if (global.flag[_tenna_fw_i] != _tenna_fw_shadow[_tenna_fw_i])
        {
            var _tenna_fw_old = _tenna_fw_shadow[_tenna_fw_i];
            var _tenna_fw_new = global.flag[_tenna_fw_i];
            _tenna_fw_shadow[_tenna_fw_i] = _tenna_fw_new;
            
            var _tenna_fw_elapsed = (current_time - _tenna_fw_start_time) / 1000;
            var _tenna_fw_mins = floor(_tenna_fw_elapsed / 60);
            var _tenna_fw_secs = floor(_tenna_fw_elapsed) mod 60;
            var _tenna_fw_ts = string(_tenna_fw_mins) + "":"" + ((_tenna_fw_secs < 10) ? ""0"" : """") + string(_tenna_fw_secs);
            
            for (var _tenna_fw_j = _tenna_fw_max_log - 1; _tenna_fw_j > 0; _tenna_fw_j--)
                _tenna_fw_log[_tenna_fw_j] = _tenna_fw_log[_tenna_fw_j - 1];
            
            _tenna_fw_log[0] = ""["" + _tenna_fw_ts + ""] Flag["" + string(_tenna_fw_i) + ""]: "" + string(_tenna_fw_old) + "" -> "" + string(_tenna_fw_new);
            _tenna_fw_log_count++;
        }
    }
}
";

bool hasScr_debug = Data.Scripts.ByName("scr_debug") is not null;
string debugCheck = hasScr_debug ? "scr_debug()" : "(variable_global_exists(\"debug\") && global.debug)";

string drawDisplay = $@"
if ({debugCheck})
{{
    draw_set_font(fnt_main);
    draw_set_halign(fa_right);
    var _tenna_fw_yoff = 8;
    for (var _tenna_fw_i = 0; _tenna_fw_i < _tenna_fw_max_log; _tenna_fw_i++)
    {{
        if (_tenna_fw_log[_tenna_fw_i] != """")
        {{
            draw_set_color(c_black);
            draw_text(633, _tenna_fw_yoff + 1, _tenna_fw_log[_tenna_fw_i]);
            draw_set_color(c_yellow);
            draw_text(632, _tenna_fw_yoff, _tenna_fw_log[_tenna_fw_i]);
            _tenna_fw_yoff += 14;
        }}
    }}
    draw_set_halign(fa_left);
    draw_set_color(c_white);
}}
";

try
{
  importGroup.QueueReplace(createCode, GetDecompiledText(createCode) + createInit);
  importGroup.QueueReplace(stepCode, GetDecompiledText(stepCode) + stepCheck);
  importGroup.QueueReplace(drawCode, GetDecompiledText(drawCode) + drawDisplay);
  
  importGroup.Import();
  ScriptMessage("Flag Watcher installed!\n\nEnable debug mode to see flag changes.");
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
}
