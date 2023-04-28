if SERVER then return end

-- [ CONFIG SECTION ]

-- if enabled, F1-F10 keys would change characters. NOTE: If enabled, console will be bound to F11 instead of F3
UseFunctionKeys = true 

-- if enabled, 1,2..0 keys would change characters
UseAltPlusDigits = false

-- if enabled, prefix would be added for all non-player characters
AddNumberPrefixes = true

-- if enabled, setclientcharacter would be used to change into a bot. NOTE: player-controlled character will not acquire AI when you're playing as bot.
EnableInMultiplayer = true

-- [ END OF CONFIG SECTION ]

LuaUserData.MakeFieldAccessible(Descriptors["Barotrauma.CrewManager"], "crewList")
DebugConsole = LuaUserData.CreateStatic("Barotrauma.DebugConsole")

CancelConsole = true
CancelUpdateSlotInput = false
SwitchToSlot = -1
ConsoleHookIdentifier = nil


Hook.Patch("Barotrauma.Character", "ControlLocalPlayer", function (instance, ptable)
    if Character.DisableControls or not Character.Controlled then
        return
    end

    if UseFunctionKeys then
        CheckFunctionKeys()
    end

    if UseAltPlusDigits then
        CheckDigitKeys()
    end
end, Hook.HookMethodType.Before)

Hook.Patch("Barotrauma.Character", "ControlLocalPlayer", function (instance, ptable)
    ChangeCharacter()
end, Hook.HookMethodType.After)

function CheckDigitKeys()
    if not PlayerInput.IsAltDown() then
        return
    end
    CancelUpdateSlotInput = true

    if PlayerInput.KeyHit(Keys.D1) then
        SwitchToSlot = 0
    elseif PlayerInput.KeyHit(Keys.D2) then
        SwitchToSlot = 1
    elseif PlayerInput.KeyHit(Keys.D3) then
        SwitchToSlot = 2
    elseif PlayerInput.KeyHit(Keys.D4) then
        SwitchToSlot = 3
    elseif PlayerInput.KeyHit(Keys.D5) then
        SwitchToSlot = 4
    elseif PlayerInput.KeyHit(Keys.D6) then
        SwitchToSlot = 5
    elseif PlayerInput.KeyHit(Keys.D7) then
        SwitchToSlot = 6
    end
end

function CheckFunctionKeys()
    if PlayerInput.KeyHit(Keys.F1) then
        SwitchToSlot = 0
    elseif PlayerInput.KeyHit(Keys.F2) then
        SwitchToSlot = 1        
    elseif PlayerInput.KeyHit(Keys.F3) and not PlayerInput.IsCtrlDown() then
        SwitchToSlot = 2
        CancelConsole = true
    elseif (PlayerInput.KeyHit(Keys.F3) and PlayerInput.IsCtrlDown()) or PlayerInput.KeyHit(Keys.F11) then
        CancelConsole = false
        DebugConsole.Toggle()
    elseif PlayerInput.KeyHit(Keys.F4) then
        SwitchToSlot = 3
    elseif PlayerInput.KeyHit(Keys.F5) then
        SwitchToSlot = 4
    elseif PlayerInput.KeyHit(Keys.F6) then
        SwitchToSlot = 5
    elseif PlayerInput.KeyHit(Keys.F7) then
        SwitchToSlot = 6
    elseif PlayerInput.KeyHit(Keys.F8) then
        SwitchToSlot = 7
    elseif PlayerInput.KeyHit(Keys.F9) then
        SwitchToSlot = 8
    elseif PlayerInput.KeyHit(Keys.F10) then
        SwitchToSlot = 9
    end
end

function UpdateUI_CharacterIndexPrefixes()
    if not AddNumberPrefixes then
        return
    end

    local char_frames = Game.GameSession.CrewManager.crewList
        .GetChild(Int32(0)).GetChild(Int32(0))

    for i=0, char_frames.CountChildren-1 do
        local frame = char_frames.GetChild(Int32(i))
        local nameEl = frame.FindChild("name", true)
        local character = frame.UserData
        local resultStr
        if (character.IsPlayer) then
            resultStr = character.Name
        else 
            resultStr = tostring(i + 1) .. ") " .. character.Name
        end
        nameEl.Text = ToolBox.LimitString(resultStr, nameEl.Font, Int32(nameEl.Rect.Width))
    end
end

function ChangeCharacter()
    if SwitchToSlot == -1 then
        return
    end
    local idx = Int32(SwitchToSlot)
    SwitchToSlot = -1

    local characterFrame = Game.GameSession.CrewManager.crewList.Content.GetChild(idx)
    if characterFrame == nil or characterFrame.UserData.IsPlayer then
        return
    end
    local character = characterFrame.UserData

    print("switching to " .. tostring(character.Name) .. " (" .. tostring(idx) .. ")" )
    PerformCharacterChange(character)
end

function PerformCharacterChange(character)
    if Game.GameSession.CrewManager.IsSinglePlayer then
        Game.GameSession.CrewManager.CharacterClicked(nil, character)
    elseif EnableInMultiplayer then
        if character.IsDead or character.IsUnconscious or not character.IsOnPlayerTeam then
            return
        end
        local command = "setclientcharacter \"" .. Game.Client.Name .. "\" \"" .. character.Name .. "\""
        print(command)
        Game.Client.SendConsoleCommand(command)
    end
end


if (UseFunctionKeys) then
    Hook.Patch("Barotrauma.DebugConsole", "Toggle", function (instance, ptable)
        if GUI.InputBlockingMenuOpen or LuaUserData.TypeOf(Screen.Selected) ~= "Barotrauma.GameScreen" then
            return
        end

        if CancelConsole then
            ptable.PreventExecution = true
        end
        CancelConsole = true
    end, Hook.HookMethodType.Before)

    Hook.Patch("Barotrauma.DebugConsole", "Toggle", function (instance, ptable)        
        if DebugConsole.IsOpen then
            AddConsoleHook()
        else
            RemoveConsoleHook()
        end
    end, Hook.HookMethodType.After)

    Hook.Patch("Barotrauma.SubEditorScreen", "Update", { "System.Single" }, function(instance, ptable)
        if PlayerInput.KeyHit(Keys.F11) then
            CancelConsole = false
            DebugConsole.Toggle()
        end
    end, Hook.HookMethodType.After)

    function OnConsoleUpdate()
        if PlayerInput.KeyHit(Keys.F11) then
            CancelConsole = false
            DebugConsole.Toggle()
        end
    end
    
    function AddConsoleHook()
        if ConsoleHookIdentifier ~= nil then
            return
        end
        ConsoleHookIdentifier = Hook.Patch("Barotrauma.DebugConsole", "Update", OnConsoleUpdate, Hook.HookMethodType.After)
    end

    function RemoveConsoleHook()
        if ConsoleHookIdentifier == nil then
            return
        end

        Hook.RemovePatch(ConsoleHookIdentifier, "Barotrauma.DebugConsole", "Update", Hook.HookMethodType.After)
        ConsoleHookIdentifier = nil
    end
end

if (UseAltPlusDigits) then
    Hook.Patch("Barotrauma.CharacterInventory", "UpdateSlotInput", function (instance, ptable)
        if CancelUpdateSlotInput then
            ptable.PreventExecution = true
            CancelUpdateSlotInput = false
            return
        end
    end, Hook.HookMethodType.Before)
end

if (AddNumberPrefixes) then
    Hook.Patch("Barotrauma.CrewManager", "AddCharacterToCrewList", {"Barotrauma.Character"}, function(instance,ptable)
        UpdateUI_CharacterIndexPrefixes()
    end, Hook.HookMethodType.After)

    Hook.Patch("Barotrauma.CrewManager", "UpdateCrewListIndices", function(instance,ptable)
        UpdateUI_CharacterIndexPrefixes()
    end, Hook.HookMethodType.After)

    Hook.Patch("Barotrauma.CrewManager", "CharacterClicked", { "Barotrauma.GUIComponent", "System.Object" }, function (instance, ptable)
        UpdateUI_CharacterIndexPrefixes()
    end, Hook.HookMethodType.After)
end

if(EnableInMultiplayer) then
    Hook.Patch("Barotrauma.Networking.GameClient", "set_Character", function (instance, ptable)
        UpdateUI_CharacterIndexPrefixes()
    end, Hook.HookMethodType.After);
end

Hook.Patch("Barotrauma.CrewManager", "OnCrewListRearranged", {
    "Barotrauma.GUIListBox", "System.Object"
},
function (instance, ptable)
    UpdateUI_CharacterIndexPrefixes()
    local crewList = ptable["crewList"]

    if not EnableInMultiplayer or instance.IsSinglePlayer or crewList.HasDraggedElementIndexChanged then
        return
    end
    local character = ptable["draggedElementData"]

    if crewList ~= instance.crewList then
        return
    end

    if instance.IsSinglePlayer or character.IsRemotePlayer then
        return
    end

    if character.IsDead or character.IsUnconscious or not character.IsOnPlayerTeam then
        return
    end

    PerformCharacterChange(character)
end, Hook.HookMethodType.After)