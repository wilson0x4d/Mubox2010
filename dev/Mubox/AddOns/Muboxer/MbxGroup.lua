function Mubox.Group.Write(message)
	if (Mubox.Group.Leader ~= nil) then
		SendChatMessage(message, "WHISPER", nil, Mubox.Group.Leader);
	end
end

function Mubox.Group.FollowCommandHandler()
	Mubox.Group.RaiseEvent("SET_LEADER");
end

function Mubox.Group.GroupNameCommandHandler(groupName)
	local oldGroupName = Mubox.Persistence.Group.Name;
	Mubox.Persistence.Group.Name = groupName;
	Mubox.Player.Write("Muboxer Group changed from \""..Mubox.Resources.Colors.TextHighlight..oldGroupName..Mubox.Resources.Colors.Text.."\" to \""..Mubox.Resources.Colors.TextHighlight..Mubox.Persistence.Group.Name..Mubox.Resources.Colors.Text.."\"");
end

function Mubox.Group.FollowGroupLeader()
	if ((not Mubox.Player.IsFollowing) and (Mubox.Group.Leader ~= nil)) then
		if ((UnitExists(Mubox.Group.Leader) ~= nil) and (UnitInRange(Mubox.Group.Leader) ~= nil)) then
			FollowUnit(Mubox.Group.Leader);
			Mubox.HUD.Write("Following "..Mubox.Group.Leader);
		else
			Mubox.Group.Write(Mubox.Group.Leader.." does not exist, or is too far to follow.");
		end
	end
end

function Mubox.Group.OnSetLeader(unitName)
	if (unitName == Mubox.Player.Name) then
		unitName = nil;
	end
	if ((unitName == nil) or UnitInParty(unitName) or UnitInRaid(unitName) or UnitInBattleground(unitName)) then
		Mubox.Group.Leader = unitName;
		if (unitName ~= nil) then		
			Mubox.MacroizeMBXINVITE(unitName);
			if ((IsPartyLeader() or IsRealPartyLeader() or IsRaidLeader() or IsRealRaidLeader())) then
				PromoteToLeader(unitName, true);
			end
		end
		Mubox.Player.IsFollowing = false;
		Mubox.Group.FollowGroupLeader();
	end
end

function Mubox.Group.OnSetTarget(unitName)
	if (unitName == Mubox.Player.Name) then
		unitName = nil;
	end
	if ((unitName == nil) or UnitInParty(unitName) or UnitInRaid(unitName) or UnitInBattleground(unitName)) then
		Mubox.MacroizeMBXTARGET(unitName);
	end
	Mubox.MacroizeMBXFOCUS(Mubox.Group.Leader);
end

function Mubox.Group.RaiseEvent(eventName)
	local target = Mubox.Group.GetTargetType("player");
	SendAddonMessage("MBXINF", Mubox.Persistence.Group.Name.." "..eventName.." "..Mubox.Player.Name, target);
end

function Mubox.Group.SetTarget()
	Mubox.Group.RaiseEvent("SET_TARGET");
end

function Mubox.Group.MassInvite(unitName)
	Mubox.MacroizeMBXINVITE(unitName);
end

function Mubox.Group.GetTargetType(unitName)
	if (UnitInBattleground(unitName)) then
		return "BATTLEGROUND";
	elseif (GetNumRaidMembers() > 0) then
		return "RAID";
	else
		return "PARTY";
	end
end

