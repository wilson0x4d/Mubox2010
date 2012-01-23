function Mubox.Player.Write(message)
	if (message ~= nil) then
		DEFAULT_CHAT_FRAME:AddMessage(Mubox.Resources.Strings.LogPrefix..message);
	end
end

function Mubox.Player.WriteHelpText()
	Mubox.Player.Write("Muboxer Version "..Mubox.Version);
	Mubox.Player.Write("  Usage: /minvite [unitName]");
	Mubox.Player.Write("       adds or removes unit from MBXINVITE Macro");
	Mubox.Player.Write("       prefix with a '-' character to remove the Unit");
	Mubox.Player.Write("  Usage: /mfollow");
	Mubox.Player.Write("       instructs all peers to follow you");
	Mubox.Player.Write("       also sets you as party/raid leader");
	Mubox.Player.Write("  Usage: /mgroup [name]");
	Mubox.Player.Write("       sets the group name for the current peer");
	Mubox.Player.Write("       a peer can only belong to one group");
	Mubox.Player.Write("  Usage: /mbx vendor [quality]");
	Mubox.Player.Write("       where 'quality' is numeric, one of:");
	Mubox.Player.Write("       0 - Junk (Grays)");
	Mubox.Player.Write("       1 - Normal (Whites)");
	Mubox.Player.Write("       2 - Uncommon (Greens)");
	Mubox.Player.Write("       3 - Rare (Blue)");
	Mubox.Player.Write("       4 - Epic (Purple)");
	Mubox.Player.Write("       5 - Legendary (Orange)");
	Mubox.Player.Write("  Usage: /mbx [command]");
	Mubox.Player.Write("       where 'command' is one of:");
	Mubox.Player.Write("       'on' - Turns Muboxer On");
	Mubox.Player.Write("       'off' - Turns Muboxer Off");
end

function Mubox.Player.MuboxerCommandHandler(parm)
	local cmd_start, cmd_stop, cmd_text, cmd_parm=string.find(string.lower(parm), "(%w+) (%w+)");
	if (cmd_text == nil) then
		cmd_start, cmd_stop, cmd_text, cmd_parm=string.find(string.lower(parm), "(%w+)");
	end
      
	if ((cmd_text == nil) or (cmd_text == "help")) then
		Mubox.Player.WriteHelpText();
		return;
	end

	-- enable/disable Muboxer
	if (cmd_text == "on") then
		Mubox.Persistence.IsEnabled = true;
		Mubox.HUD.Write("Muboxer Enabled");
	elseif (cmd_text == "off") then
		Mubox.Persistence.IsEnabled = false;
		Mubox.HUD.Write("Muboxer Disabled");
	elseif (cmd_text == "vendor") then
		if (cmd_parm ~= nil) then
			local qualityLevel = tonumber(cmd_parm);
			if (qualityLevel ~= nil) then
				if ((qualityLevel >= 0) and (qualityLevel <= 5)) then
					Mubox.Persistence.MaxQualityAutoVendor = qualityLevel;
					Mubox.Player.Write("AutoVendor Quality Set to \""..Mubox.Resources.Colors.TextHighlight..qualityLevel..Mubox.Resources.Colors.Text.."\"");
					return;
				end
			end
		end
		Mubox.Player.Write("  Usage: /mbx vendor [quality]");
		Mubox.Player.Write("       where 'quality' is numeric, one of:");
		Mubox.Player.Write("       0 - Junk (Grays)");
		Mubox.Player.Write("       1 - Normal (Whites)");
		Mubox.Player.Write("       2 - Uncommon (Greens)");
		Mubox.Player.Write("       3 - Rare (Blue)");
		Mubox.Player.Write("       4 - Epic (Purple)");
		Mubox.Player.Write("       5 - Legendary (Orange)");
	elseif (cmd_text == "reload") then
		ReloadUI();
	elseif (cmd_text == "sort") then
		Mubox.Inventory.Bags.Sort();
	end
end
