function Mubox.PvP.EndJoinBattleground(self, event)
	local joinAsGroup = CanJoinBattlefieldAsGroup();
	if ((Mubox.Group.Leader == nil) and joinAsGroup) then
		local rated = false;
		JoinBattlefield(0, joinAsGroup, rated);
	end
	self:UnregisterEvent("PVPQUEUE_ANYWHERE_SHOW");
	if (self == Mubox.PvP.Frame) then
		Mubox.PvP.Frame = nil;
	end
end

function Mubox.PvP.BeginJoinBattleground(id)
	if (not id) then
		id = 1; -- Alterac
	end
	local maxBattlegroundTypes = GetNumBattlegroundTypes();
	for index = 1, maxBattlegroundTypes do
		local name, canEnter, isHoliday, isRandom, battlegroundId = GetBattlegroundInfo(index);
		if (canEnter and (battlegroundId == id)) then
			local status = GetBattlefieldStatus(battlegroundId);
			if (status == nil) then
				status = "none";
			end
			if ((not canEnter) or (status == "active") or (status == "queued") or (status == "confirm")) then
				return;
			end
			local frame = Mubox.PvP.Frame;
			if (frame == nil) then
				frame = CreateFrame("FRAME");
				frame:SetScript("OnEvent", Mubox.PvP.EndJoinBattleground);
				Mubox.PvP.Frame = frame;
			end
			if (frame ~= nil) then
				if (not frame:IsEventRegistered("PVPQUEUE_ANYWHERE_SHOW")) then
					frame:RegisterEvent("PVPQUEUE_ANYWHERE_SHOW");
					RequestBattlegroundInstanceInfo(index);
				end
			end
			return;
		end
	end
end

function Mubox.PvP.TryQueuePvP()
	-- defer
	if ((Mubox.PvP.NextCheckTime ~= nil) and (Mubox.PvP.NextCheckTime > time())) then
		return;
	end
	Mubox.PvP.NextCheckTime = time() + 20; -- TODO: config, allow another check in 20 seconds

	-- constrain
	if (not Mubox.Persistence.IsEnabled) then
		return;
	end
	if (Mubox.Persistence.PvP.Battleground.AutoQueue == nil) then
		return;
	end
	if (UnitInBattleground("player")) then
		return;
	end

	-- execute
	Mubox.PvP.BeginJoinBattleground(Mubox.Persistence.PvP.Battleground.AutoQueue);
end

function Mubox.PvP.DumpBattlegroundInfo(parm)
	local cmd_start, cmd_stop, cmd_text, cmd_parm=string.find(string.lower(parm), "(%w+) (%w+)");
	if (cmd_text == nil) then
		cmd_start, cmd_stop, cmd_text, cmd_parm=string.find(string.lower(parm), "(%w+)");
	end
	
	if (cmd_text ~= nil) then
		if (cmd_text == "off") then
			Mubox.Persistence.PvP.Battleground.AutoQueue = nil;
		else
			Mubox.Persistence.PvP.Battleground.AutoQueue = tonumber(cmd_text);
		end
	end

	local maxBattlegroundTypes = GetNumBattlegroundTypes();
	for battlegroundTypeId = 1, maxBattlegroundTypes do
		local name, canEnter, isHoliday, isRandom, battlegroundId = GetBattlegroundInfo(battlegroundTypeId);
		if (name ~= nil) then
			local status = GetBattlefieldStatus(battlegroundTypeId);
			if (status == nil) then
				status = "none";
			end
			if (battlegroundId == nil) then
				battlegroundId = 0;
			end
			local logString = "BG ["..battlegroundTypeId.."] "..battlegroundId.."/"..name.." ("..status..")";
			if (isHoliday) then
				logString = logString.." (holiday)";
			end
			if (not canEnter) then
				logString = logString.." (locked)";
			end
			if (battlegroundId == Mubox.Persistence.PvP.Battleground.AutoQueue) then
				logString = logString.." (AUTO)";
				Mubox.HUD.Write("BG Auto Queue Enabled for '"..name.."'");
			end
			Mubox.Player.Write(logString);
		end
	end

	if (not Mubox.Persistence.PvP.Battleground.AutoQueue) then
		Mubox.HUD.Write("BG Auto Queueing Disabled");
	end
end
