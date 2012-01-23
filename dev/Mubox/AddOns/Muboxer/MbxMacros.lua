function Mubox.MacroizeMBXFOLLOW()
	Mubox.Util.CreateOrEditMacro("MBXFOLLOW", "/mfollow");
end

function Mubox.MacroizeMBXFOCUS(unitName)
	if (unitName ~= nil) then
		Mubox.Util.CreateOrEditMacro("MBXFOCUS", "/focus "..unitName.."\r\n/assist focus");
	else
		Mubox.Util.CreateOrEditMacro("MBXFOCUS", "/clearfocus");
	end
end

function Mubox.MacroizeMBXTARGET(unitName)
	if (unitName ~= nil) then
		Mubox.Util.CreateOrEditMacro("MBXTARGET", "/target "..unitName.."-target");
	else
		Mubox.Util.CreateOrEditMacro("MBXTARGET", "/script SetRaidTarget(\"target\", 8);");
	end
end

function Mubox.MacroizeMBXINVITE(unitName)
	-- manage invite list if unit name was supplied
	if ((unitName ~= nil) and (string.len(unitName) > 0)) then
		if (string.sub(unitName, 1, 1) ~= "-") then
			-- CreateOrUpdate
			if (Mubox.Persistence.InviteList == nil) then
				-- first-time use, initialize
				Mubox.Persistence.InviteList = " "..string.lower(unitName).." ";
			elseif (string.find(Mubox.Persistence.InviteList, "%s"..string.lower(unitName).."%s") == nil) then
				-- add if not exists
				Mubox.Persistence.InviteList = Mubox.Persistence.InviteList..string.lower(unitName).." ";
			end
		else
			-- Delete
			if (Mubox.Persistence.InviteList ~= nil) then
				local adjustedUnitName = string.sub(unitName, 2, string.len(unitName) - 1);
				adjustedUnitName = "%s"..string.lower(adjustedUnitName).."%s";
				Mubox.Persistence.InviteList = string.gsub(Mubox.Persistence.InviteList, adjustedUnitName, " ");
			end
		end
	end
	-- regenerate macro from current invite list
	local it_MbxInviteList = Mubox.Persistence.InviteList;
	if (it_MbxInviteList ~= nil) then
		local macro = "/mfollow\r\n";
		for param in string.gmatch(it_MbxInviteList, "[^%s]+") do
			macro = macro.."/invite "..param.."\r\n";
		end			
		if (macro ~= "/mfollow\r\n") then
			macro = macro.."/mfollow\r\n";
		end
		Mubox.Util.CreateOrEditMacro("MBXINVITE", macro);
	end
end
