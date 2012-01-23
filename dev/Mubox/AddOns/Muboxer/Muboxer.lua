function Mubox.OnLoad()
	SLASH_MUBOXER1="/mbx";
	SLASH_MUBOXER2="/mubox";
	SLASH_MGROUP1 = "/mgroup";
	SLASH_MFOLLOW1 = "/mfollow";
	SLASH_MINVITE1 = "/minvite";
	SLASH_MSORT1 = "/msort";
	SLASH_MBG1 = "/mbg";
	SlashCmdList["MUBOXER"] = Mubox.Player.MuboxerCommandHandler;
	SlashCmdList["MFOLLOW"] = Mubox.Group.FollowCommandHandler;
	SlashCmdList["MGROUP"] = Mubox.Group.GroupNameCommandHandler;
	SlashCmdList["MINVITE"] = Mubox.Group.MassInvite;
	SlashCmdList["MSORT"] = Mubox.Inventory.Bags.Sort;
	SlashCmdList["MBG"] = Mubox.PvP.DumpBattlegroundInfo;
	for _,v in pairs(Mubox.Resources.EventNames) do
		Muboxer:RegisterEvent(v)
	end
	RegisterAddonMessagePrefix("MBXINF")
end

function Mubox.OnUpdate(self, elapsed)
	if ((not Mubox.Persistence) or (not Mubox.Persistence.IsEnabled)) then
		return;
	end

	local timeNow = time();
	if (Mubox.TimeOfNextUpdate == nil) then
		Mubox.TimeOfNextUpdate = timeNow;
	end
	if (Mubox.TimeOfNextUpdate >= timeNow) then
		return;
	end
	-- TODO: apparently non-Windows is in milliseconds?
	Mubox.TimeOfNextUpdate = time() + 5; -- NOTE: we only allow updates once every 5 seconds

	-- note: we only begin one process per tick, e.g. sorting bags, or queuing for a bg, etc. these are in priority. thus, bag sorts will suppress queuing for a bg.
	if (Mubox.Inventory.Bags.ShouldSort) then
		Mubox.Inventory.Bags.Sort(4, 1);
	else
		Mubox.PvP.TryQueuePvP();
	end
end

function Mubox.OnEvent(self, event, ...)
	local arg1, arg2, arg3, arg4, arg5 = ...;
	if ((not Mubox.Persistence) or (not Mubox.Persistence.IsEnabled)) then
		if (event == "ADDON_LOADED") then
			if (arg1 ~= "Muboxer") then
				return;
			end
			if (MuboxPersistence ~= nil) then
				Mubox.Persistence = MuboxPersistence;
				if (Mubox.Persistence.PvP == nil) then
					Mubox.Persistence.PvP =
					{
						Battleground =
						{
							AutoQueue = nil
						},
						Arena = 
						{
							AutoQueue = nil
						}
					};
				end
			else
				Mubox.Persistence = 
				{
					IsEnabled = false,
					Player =
					{
						XP = 
						{
							Total = 0,
							Needed = 0
						},
						Faction = "",
						Level = 0,
						Name = "",
						Class = "",
						Realm = "",
						GuildName = "",
						Money = 0
					},
					Group =
					{
						Name = "DEFAULT"
					},
					PvP =
					{
						Battleground =
						{
							AutoQueue = nil
						},
						Arena = 
						{
							AutoQueue = nil
						}
					},
					MaxQualityAutoVendor = 0,
					InviteList = nil
				};
			end
			if (not Mubox.Persistence.IsEnabled) then
				Mubox.Player.MuboxerCommandHandler("");
				Mubox.HUD.Write("Muboxer "..Mubox.Version.." is Currently Disabled.");
			else
				Mubox.HUD.Write(Mubox.Resources.Colors.Text.."Muboxer "..Mubox.Version.." is using Group \""..Mubox.Resources.Colors.TextHighlight..Mubox.Persistence.Group.Name..Mubox.Resources.Colors.Text.."\"");
			end
			Mubox.Persistence.Player.Class = UnitClass("player");
			Mubox.Persistence.Player.Level = UnitLevel("player");
		end
		return;
	elseif (event == "BAG_UPDATE") then
		Mubox.Inventory.Bags.Sort(4, 1);
	elseif (event == "ITEM_LOCK_CHANGED") then
		local ok, _, _, leftLocked = pcall(GetContainerItemInfo, arg1, arg2);
		if (ok) then
			if (not leftLocked) then
				Mubox.Inventory.Bags.Sort(arg1, arg2);
			else
				Mubox.Inventory.Bags.Sort(4, 1);
			end
		end
	elseif (event == "PARTY_LOOT_METHOD_CHANGED") then
		Mubox.Player.Write("Muboxer Group is \""..Mubox.Resources.Colors.TextHighlight..Mubox.Persistence.Group.Name..Mubox.Resources.Colors.Text.."\"");
		Mubox.MacroizeMBXFOLLOW();
		Mubox.MacroizeMBXTARGET(nil);
		Mubox.MacroizeMBXFOCUS(nil);
	elseif (event == "PLAYER_TARGET_CHANGED") then
		if (arg1 == "up") then
			if (UnitExists("target")) then
				Mubox.Group.SetTarget();
			end
		end
	elseif (event == "CHAT_MSG_ADDON") then
		local args = { };
		local idx = 0;
		local subEvent = nil;
		for param in string.gmatch(arg2, "[^%s]+") do
			if (idx == 0) then
				if (param ~= Mubox.Persistence.Group.Name) then
					return;
				end
			elseif (idx == 1) then
				subEvent = param;
			else
				table.insert(args, param);
			end
			idx = idx + 1;
		end
		if (subEvent == "SET_LEADER") then
			Mubox.Group.OnSetTarget(args[1]);
			Mubox.Group.OnSetLeader(args[1]);
		elseif (subEvent == "SET_TARGET") then
			Mubox.Group.OnSetTarget(args[1]);
		end
	elseif (event == "PLAYER_XP_UPDATE") then
		OnPlayerXPUpdate();
	elseif (event == "AUTOFOLLOW_BEGIN") then
		OnAutoFollowBegin();
	elseif (event == "AUTOFOLLOW_END") then
		OnAutoFollowEnd();
	elseif (event == "PLAYER_LEAVE_COMBAT") then
		OnPlayerLeaveCombat();
	elseif (event == "PLAYER_REGEN_ENABLED") then
		OnPlayerLeaveCombat();
	elseif (event == "UNIT_SPELLCAST_CHANNEL_STOP") then
		OnUnitSpellcastChannelStop();
	elseif (event == "LOOT_CLOSED") then
		OnLootClosed();
	elseif (event == "ZONE_CHANGED_NEW_AREA") then
		OnZoneChangedNewArea();
	elseif ((event == "CHAT_MSG_WHISPER") or (event == "CHAT_MSG_PARTY") or (event == "CHAT_MSG_RAID")) then
		-- accept follow invitations from party/raid members
		if (string.find(string.lower(arg1), "follow me", 1, true) ~= nil) then
			Mubox.Group.OnSetLeader(arg2);
		end
	elseif (event == "UI_ERROR_MESSAGE") then
		if (Mubox.Group.Leader ~= nil) then
			if ((arg1 == ERR_AUTOFOLLOW_TOO_FAR) or (arg1 == ERR_UNIT_NOT_FOUND)) then
				Mubox.Group.Write(arg1);
			end
		end
	elseif (event == "PLAYER_LOGOUT") then
		OnPlayerLogout();
	elseif (event == "PLAYER_CONTROL_GAINED") then
		OnPlayerControlGained();
	elseif (IsShiftKeyDown()) then
		return;
	elseif (event == "GOSSIP_SHOW") then
		OnGossipShow();
	elseif (event == "QUEST_GREETING") then
		OnQuestGreeting();
	elseif (event == "QUEST_DETAIL") then
		OnQuestDetail();
	elseif (event == "QUEST_ACCEPT_CONFIRM") then
		ConfirmAcceptQuest();
	elseif (event == "QUEST_PROGRESS") then
		OnQuestProgress();
	elseif (event == "QUEST_COMPLETE") then
		OnQuestComplete();
	elseif (event == "QUEST_FINISHED") then
		OnQuestFinished();
	elseif (event == "MERCHANT_SHOW") then
		OnMerchantShow();
	elseif (event == "PARTY_INVITE_REQUEST") then
		OnPartyInvite();
	elseif (event == "PARTY_MEMBERS_CHANGED") then
		OnPartyMembersChanged();
	end
end

-- TODO: AutoTrade function to trade profession materials, negotiated via addon chat

function OnGossipShow()
	local L_unitName = UnitName("npc");
	if ((Mubox.Player.NPC.Name ~= L_unitName) or (Mubox.Player.NPC.ActiveCount ~= GetNumGossipActiveQuests()) or (Mubox.Player.NPC.AvailableCount ~= GetNumGossipAvailableQuests())) then
		Mubox.Player.NPC.Name = L_unitName;
		Mubox.Player.NPC.ActiveCount = GetNumGossipActiveQuests();
		Mubox.Player.NPC.ActiveIndex = 0;
		Mubox.Player.NPC.AvailableCount = GetNumGossipAvailableQuests();
		Mubox.Player.NPC.AvailableIndex = 0;						
		Mubox.Player.NPC.TrainerCount = GetNumTrainerServices();
		Mubox.Player.NPC.TrainerIndex = 0;						
		Mubox.Player.NPC.OptionCount = GetNumGossipOptions();
		Mubox.Player.NPC.OptionIndex = 0;						
	end
	if (Mubox.Player.NPC.ActiveIndex < Mubox.Player.NPC.ActiveCount) then
		Mubox.Player.NPC.ActiveIndex = Mubox.Player.NPC.ActiveIndex + 1;
		SelectGossipActiveQuest(Mubox.Player.NPC.ActiveIndex);
	elseif (Mubox.Player.NPC.AvailableIndex < Mubox.Player.NPC.AvailableCount) then
		Mubox.Player.NPC.AvailableIndex = Mubox.Player.NPC.AvailableIndex + 1;
		SelectGossipAvailableQuest(Mubox.Player.NPC.AvailableIndex);
	elseif (Mubox.Player.NPC.TrainerIndex < Mubox.Player.NPC.TrainerCount) then
		Mubox.Player.NPC.TrainerIndex = Mubox.Player.NPC.TrainerIndex + 1;
		OnGossipShow(); -- iterate services
	elseif (Mubox.Player.NPC.OptionCount == 0) then
		-- do not close trainer frames that open as gossip frames
		if (Mubox.Player.NPC.TrainerCount == 0) then
			CloseGossip();
			Mubox.Group.FollowGroupLeader();
		end
	end
end

function OnQuestGreeting()
	local L_unitName = UnitName("npc");
	if ((Mubox.Player.NPC.Name ~= L_unitName) or (Mubox.Player.NPC.ActiveCount ~= GetNumActiveQuests()) or (Mubox.Player.NPC.AvailableCount ~= GetNumAvailableQuests())) then
		Mubox.Player.NPC.Name = L_unitName;
		Mubox.Player.NPC.ActiveCount = GetNumActiveQuests();
		Mubox.Player.NPC.ActiveIndex = 0;
		Mubox.Player.NPC.AvailableCount = GetNumAvailableQuests();
		Mubox.Player.NPC.AvailableIndex = 0;						
	end
	if (Mubox.Player.NPC.ActiveIndex < Mubox.Player.NPC.ActiveCount) then
		Mubox.Player.NPC.ActiveIndex = Mubox.Player.NPC.ActiveIndex + 1;
		SelectActiveQuest(Mubox.Player.NPC.ActiveIndex);
	elseif (Mubox.Player.NPC.AvailableIndex < Mubox.Player.NPC.AvailableCount) then
		Mubox.Player.NPC.AvailableIndex = Mubox.Player.NPC.AvailableIndex + 1;
		SelectAvailableQuest(Mubox.Player.NPC.AvailableIndex);
	elseif (Mubox.Player.NPC.OptionCount == 0) then
		CloseQuest();
		Mubox.Group.FollowGroupLeader();
	end
end

function OnQuestComplete()
	local num_choices = GetNumQuestChoices();
	if (num_choices == nil) then
		GetQuestReward(nil);
	elseif (num_choices <= 1) then
		GetQuestReward(1);
	elseif (num_choices > 1) then
		local preferredChoiceIndex = 0;
		for choiceIndex=1,num_choices do
			local itemLink = GetQuestItemLink("choice", choiceIndex);
			if (itemLink ~= nil) then
				local name, link, quality, iLevel, reqLevel, class, subclass, maxStack, equipSlot, texture, vendorPrice = GetItemInfo(itemLink);
				local itemScore = Mubox.Util.GetItemScore(link, nil, true);
				if (preferredChoiceIndex == 0) then
					preferredChoiceIndex = choiceIndex;
				else				
					local preferredName, preferredLink, preferredQuality, preferredILevel, preferredReqLevel, preferredClass, preferredSubclass, preferredMaxStack, preferredEquipSlot, preferredTexture, preferredVendorPrice = 
						GetItemInfo(GetQuestItemLink("choice", preferredChoiceIndex));
					if (preferredName ~= nil) then
						if (Mubox.Util.CompareItemScore(link, preferredLink)) then
							preferredChoiceIndex = choiceIndex;
						end
					end					
				end
			end
		end
		if (preferredChoiceIndex ~= 0) then
			local rewardLink = GetQuestItemLink("choice", preferredChoiceIndex);
			GetQuestReward(preferredChoiceIndex);			
		end
	end
end

function OnQuestDetail()
	AcceptQuest();
end

function OnQuestProgress()
	if (IsQuestCompletable()) then
		CompleteQuest();
	else
		DeclineQuest();
	end
end

function OnQuestFinished()
	-- NOP
end

function OnMerchantShow()
	Mubox.Inventory.AutoVendor();
	Mubox.Equipment.Repair();
	Mubox.Group.FollowGroupLeader();
end

function OnPartyInvite()
	Muboxer:RegisterEvent("PARTY_MEMBERS_CHANGED");
	AcceptGroup();
end

function OnPartyMembersChanged()
	StaticPopup_Hide("PARTY_INVITE");
	Muboxer:UnregisterEvent("PARTY_MEMBERS_CHANGED");
end

function OnPlayerLogout()
	Mubox.Persistence.Player.Name = UnitName("player");
	Mubox.Persistence.Player.Realm = GetRealmName("player");
	Mubox.Persistence.Player.Faction = UnitFactionGroup("player");
	Mubox.Persistence.Player.Class = UnitClass("player");
	Mubox.Persistence.Player.Level = UnitLevel("player");
	Mubox.Persistence.Player.XP.Needed = UnitXPMax("player");
	Mubox.Persistence.Player.XP.Total = UnitXP("player");
	if (GetGuildInfo("player") == nil) then
		Mubox.Persistence.Player.GuildName = "";
	else
		Mubox.Persistence.Player.GuildName = GetGuildInfo("player");
	end
	MuboxPersistence = Mubox.Persistence;
end

function OnPlayerXPUpdate()
	if (UnitXP("player") < Mubox.Persistence.Player.XP.Total) then
		Mubox.Persistence.Player.XP.Needed = UnitXPMax("player");
	end
	if (UnitLevel("player") > Mubox.Persistence.Player.Level) then
		Mubox.Persistence.Player.Level = UnitLevel("player");
		Mubox.Player.Write("Level Up! ("..Mubox.Resources.Colors.TextHighlight..Mubox.Persistence.Player.Level..Mubox.Resources.Colors.Text..")");
	end
	Mubox.Persistence.Player.XP.Total = UnitXP("player");
end

function OnPlayerControlGained()
	Mubox.Group.FollowGroupLeader();
end

function OnPlayerLeaveCombat()
	-- Mubox.Group.FollowGroupLeader();
end

function OnUnitSpellcastChannelStop()
	-- Mubox.Group.FollowGroupLeader();
end

function OnZoneChangedNewArea()
	Mubox.Group.FollowGroupLeader();
	Mubox.Player.IsFollowing = false;
end

function OnLootClosed()
	Mubox.Group.FollowGroupLeader();
end

function OnAutoFollowBegin()
	Mubox.Player.IsFollowing = true;
end

function OnAutoFollowEnd()
	Mubox.Player.IsFollowing = false;
end

