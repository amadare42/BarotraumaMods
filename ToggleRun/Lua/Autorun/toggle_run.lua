if SERVER then return end

-- [configurable section]

-- timeout in seconds to release run key after no movement keys are pressed (0 to disable)
Run_timeout = 0.1

-- if true, run key will always make charadcter run, never toggle back to walk
To_run_only = true

-- [end of configurable section]

Is_running = false
Is_waiting_for_release = false
Running_released = -1

LuaUserData.MakeFieldAccessible(LuaUserData.RegisterTypeBarotrauma("Character"), "keys") -- Character.keys
LuaUserData.MakeFieldAccessible(LuaUserData.RegisterTypeBarotrauma("Key"), "inputType") -- Key.inputType

Hook.Patch("Barotrauma.Character", "ControlLocalPlayer", {
    "System.Single", "Barotrauma.Camera", "System.Boolean"
},
function (instance, ptable)

    -- auto release run key if no movements keys are pressed for 200ms (default)
    if Run_timeout ~= 0 and Is_running and not movement_pressed(instance) then
        if Is_waiting_for_release then
            if Game.GameScreen.GameTime - Running_released > Run_timeout then
                Is_running = false
                Is_waiting_for_release = false
            end
        else 
            Is_waiting_for_release = true
            Running_released = Game.GameScreen.GameTime
        end
    else
        Is_waiting_for_release = false
    end

    -- auto press run key if running is enabled
    for key, _ in instance.keys do
        if key.inputType == InputType.Run then
            if Is_running then
                if key.Hit and not To_run_only then
                    Is_running = false
                else
                    key.Held = true
                end
            elseif key.Hit then
                Is_running = true
            end
        end
    end
end, Hook.HookMethodType.After)

function movement_pressed(character) 
    return character.IsKeyDown(InputType.Left) or character.IsKeyDown(InputType.Right) or character.IsKeyDown(InputType.Up) or character.IsKeyDown(InputType.Down)
end