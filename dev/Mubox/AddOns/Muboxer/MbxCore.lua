Mubox =
{
	Version = "1.5.0.0",
	Resources = 
	{
		Colors =
		{
			Text = "|cff9d9d9d",
			TextHighlight = "|cfff56c00",
			Purple = "|cffab44ff",
			Gold = "|cffffff00",
			Silver = "|cffc7c7c7",
			Copper = "|cffffbb33"
		},
		EventNames =
		{
			"ADDON_LOADED",
			"CHAT_MSG_ADDON",
			"PLAYER_TARGET_CHANGED",
			"CHAT_MSG_WHISPER",
			"CHAT_MSG_PARTY",
			"CHAT_MSG_RAID",
			"UI_ERROR_MESSAGE",
			"QUEST_GREETING",
			"QUEST_DETAIL",
			"QUEST_PROGRESS",
			"QUEST_COMPLETE",	
			"QUEST_ACCEPT_CONFIRM",
			"QUEST_FINISHED",
			"PARTY_INVITE_REQUEST",
			"GOSSIP_SHOW",
			"MERCHANT_SHOW",
			"PLAYER_LOGOUT",
			"PLAYER_XP_UPDATE",
			"LOOT_CLOSED",
			"PLAYER_CONTROL_GAINED",
			"PLAYER_LEAVE_COMBAT",
			"UNIT_SPELLCAST_CHANNEL_STOP",
			"PLAYER_REGEN_ENABLED",
			"AUTOFOLLOW_BEGIN",
			"AUTOFOLLOW_END",
			"ITEM_LOCK_CHANGED",
			"BAG_UPDATE",
		},
		VendorDoNotSell =
		{
			"Quest",
		},
		ClassDoNotUse =
		{
			-- 'Do Not Use' lists to determine the item subtypes to ignore during quest reward selection
			DEATHKNIGHT =
			{
				"Cloth",
				"Leather",
				"Staff",
				"Mace",
			},
			DRUID = 
			{			
			},
			HUNTER = 
			{ 
			},
			MAGE = 
			{ 
			},
			PALADIN = 
			{ 
			},
			PRIEST = 
			{
				"Mail",
				"Leather",
				"Shields",
				"Plate", 
				"One-Handed Axes",
				"Two-Handed Axes",
				"Bows",
				"Guns",
				"Polearms",
				"Crossbows",
			},
			ROGUE = 
			{ 
			},
			SHAMAN = 
			{ 
			},
			WARLOCK = 
			{
			},
			WARRIOR =
			{
			}
		},
		ClassStatFactors =
		{
			-- 'Stat Factors' determine which item stats are used to improve an item score. if not defined a stat will have no effect on item score.
			DEATHKNIGHT =
			{
				ITEM_MOD_HASTE_MELEE_RATING_SHORT = 33,
				ITEM_MOD_POWER_REGEN6_SHORT = 20,
				ITEM_MOD_STAMINA_SHORT = 10,
				ITEM_MOD_STRENGTH_SHORT = 10
			},
			DRUID = 
			{ 
			},
			HUNTER = 
			{ 
			},
			MAGE = 
			{ 
			},
			PALADIN = 
			{ 
			},
			PRIEST = 
			{ 
				ITEM_MOD_SPELL_POWER_SHORT = 2,
				ITEM_MOD_POWER_REGEN0_SHORT = 33,
				ITEM_MOD_MANA_SHORT = 2,
				ITEM_MOD_SPELL_PENETRATION_SHORT = 0.66,
				ITEM_MOD_STAMINA_SHORT = 1.5,
				ITEM_MOD_INTELLECT_SHORT = 3,
				ITEM_MOD_SPIRIT_SHORT  = 3,
				ITEM_MOD_STRENGTH_SHORT = 0.1,
				ITEM_MOD_AGILITY_SHORT = 0.1
			},
			ROGUE = 
			{ 
			},
			SHAMAN = 
			{ 
				ITEM_MOD_SPELL_POWER_SHORT = 1,
				ITEM_MOD_POWER_REGEN0_SHORT = 20,
				ITEM_MOD_MANA_SHORT = 0.5,
				ITEM_MOD_SPELL_PENETRATION_SHORT = 0.66,
				ITEM_MOD_STAMINA_SHORT = 2.5,
				ITEM_MOD_INTELLECT_SHORT = 2,
				ITEM_MOD_SPIRIT_SHORT  = 2,
				ITEM_MOD_STRENGTH_SHORT = 1.5,
				ITEM_MOD_AGILITY_SHORT = 1.5
			},
			WARLOCK = 
			{
				ITEM_MOD_SPELL_PENETRATION_SHORT = 1.75,
				ITEM_MOD_SPELL_POWER_SHORT = 1,
				ITEM_MOD_POWER_REGEN0_SHORT = 33,
				ITEM_MOD_INTELLECT_SHORT = 3,
				ITEM_MOD_STAMINA_SHORT = 1.5,
				ITEM_MOD_SPIRIT_SHORT  = 1,
				ITEM_MOD_MANA_SHORT = 1
			},
			WARRIOR =
			{
				ITEM_MOD_STRENGTH_SHORT = 3,
				ITEM_MOD_STAMINA_SHORT = 3,
				ITEM_MOD_AGILITY_SHORT = 3,
				ITEM_MOD_POWER_REGEN1_SHORT = 2
			}
		}
	},
	Player =
	{	
		Name = UnitName("player"),
		Class = UnitClass("player"),
		FocusTargetName = nil,
		IsFollowing = false,
		NPC = 
		{
			Name = "",
			ActiveCount = 0,
			ActiveIndex = 0,
			AvailableCount = 0,
			AvailableIndex = 0,
			TrainerCount = 0,
			TrainerIndex = 0,
			OptionCount = 0,
			OptionIndex = 0
		}
	},
	HUD =
	{
	},
	Inventory =
	{
		Bags =
		{
			IsBusySorting = false,
			ShouldSort = true
		}
	},
	Equipment = 
	{
		SlotNames = 
		{
			"AmmoSlot",
			"BackSlot",
			"Bag0Slot",
			"Bag1Slot",
			"Bag2Slot",
			"Bag3Slot",
			"ChestSlot",
			"FeetSlot",
			"Finger0Slot",
			"Finger1Slot",
			"HandsSlot",
			"HeadSlot",
			"LegsSlot",
			"MainHandSlot",
			"NeckSlot",
			"RangedSlot",
			"SecondaryHandSlot",
			"ShirtSlot",
			"ShoulderSlot",
			"TabardSlot",
			"Trinket0Slot",
			"Trinket1Slot",
			"WaistSlot",
			"WristSlot"
		}
	},
	Group =
	{
		Leader = nil
	},
	PvP =
	{
	},
	Util =
	{
	}	
}

Mubox.Resources.Strings = 
{
	LogPrefix = Mubox.Resources.Colors.Text.."["..Mubox.Resources.Colors.TextHighlight.."MBX"..Mubox.Resources.Colors.Text.."]: "
}

function Mubox.Util.ConvertMoneyToString(input)
	local gold = math.floor(input / (100 * 100));
	local silver = math.floor((input / (100)) - (gold * 100));
	local copper = math.floor(input - (silver * 100) - (gold * 100 * 100));
	local moneyString = "";
	if (gold > 0) then
		moneyString = moneyString..Mubox.Resources.Colors.Gold..gold.."|TInterface\\MoneyFrame\\UI-GoldIcon:14:14:2:0|t ";
	end
	if (silver > 0) then
		moneyString = moneyString..Mubox.Resources.Colors.Silver..silver.."|TInterface\\MoneyFrame\\UI-SilverIcon:14:14:2:0|t ";
	end
	if (copper > 0) then
		moneyString = moneyString..Mubox.Resources.Colors.Copper..copper.."|TInterface\\MoneyFrame\\UI-CopperIcon:14:14:2:0|t ";
	end
	return moneyString;
end

function Mubox.Util.CreateOrEditMacro(macro_name, macro_body)
	if (InCombatLockdown()) then
		return;
	end
	local index = GetMacroIndexByName(macro_name);
	if (index ~= 0) then
		EditMacro(index, macro_name, nil, macro_body);
	else
		CreateMacro(macro_name, 1, macro_body, 1);
	end
end

function Mubox.Util.CompareItemScore(item1, item2, class, log)
	return Mubox.Util.GetItemScore(item1, class, log) > Mubox.Util.GetItemScore(item2, class, log);
end

function Mubox.Util.GetItemSortKey(item)
	if (item == nil) then
		return "";
	end
	local itemName, itemLink, itemQuality, itemLevel, itemMinLevel, itemType, itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = 
		GetItemInfo(item);
	local key = "";
	if (itemType == nil) then
		key = key.."  ";
	else
		key = key..strsub(itemType, 1, 2);
	end
	if (itemSubType == nil) then
		key = key.."  ";
	else
		key = key..strsub(itemSubType, 1, 2);
	end
	if (itemQuality == nil) then
		key = key.."0";
	else
		key = key..tostring(itemQuality);
	end
	if (itemName == nil) then
		key = key.." ";
	else
		key = key..strsub(itemName, 1, 2);
	end
	return key;
end

function Mubox.Util.GetItemScore(item, class, log)
	if (item == nil) then
		return -1;
	end
	local itemName, itemLink, itemQuality, itemLevel, itemMinLevel, itemType, itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = GetItemInfo(item);
	if (itemLink == nil) then
		return -1;
	end

	local itemScoreLog = "";
	
	if (class == nil) then
		class = Mubox.Persistence.Player.Class;
	end

	local itemScore = 0;

	-- adjust score based on equipped chest armor or mainhand weapon type	
	if (itemType == "Armor") then		
		local armorInventoryId = GetInventorySlotInfo("ChestSlot");
		if (armorInventoryId ~= nil) then
			local armorItemId = GetInventoryItemID("player", armorInventoryId);
			if (armorItemId ~= nil) then
				local armorName, armorLink, armorQuality, armorLevel, armorMinLevel, armorType, armorSubType, armorStackCount, armorEquipLoc, armorTexture, armorSellPrice = GetItemInfo(armorItemId);
				if (armorSubType == itemSubType) then
					itemScoreLog = itemScoreLog..armorSubType;
					itemScore = itemScore + 100 + string.len(armorSubType);
				end
			end
		end
	elseif (itemType == "Weapon") then	
		local weaponInventoryId = GetInventorySlotInfo("MainHandSlot");
		if (weaponInventoryId ~= nil) then
			local weaponItemId = GetInventoryItemID("player", weaponInventoryId);
			if (weaponItemId ~= nil) then
				local weaponName, weaponLink, weaponQuality, weaponLevel, weaponMinLevel, weaponType, weaponSubType, weaponStackCount, weaponEquipLoc, weaponTexture, weaponSellPrice = GetItemInfo(weaponItemId);
				if (weaponSubType == itemSubType) then
					itemScoreLog = itemScoreLog..weaponSubType;
					itemScore = itemScore + 100 + string.len(weaponSubType);
				end
			end
		end
	elseif (itemType == "Container") then
		return 0;
	elseif (itemType == "Recipe") then
		return 1;
	elseif (itemType == "Trade Goods") then
		return 2;
	elseif (itemType == "Gem") then
		return 3;
	elseif (itemType == "Quest") then
		return 4;
	elseif (itemType == "Consumable") then
		itemScore = itemScore + 9000;
	elseif (itemType == "Miscellaneous") then
		itemScore = itemScore + 11000;
	end

	-- adjust score based on stat factors for specified class
	local stats = GetItemStats(itemLink);
	if (stats ~= nil) then
		for stat, value in pairs(stats) do
			local statFactors = Mubox.Resources.ClassStatFactors[string.upper(class)];
			if (statFactors ~= nil) then
				local statFactor = statFactors[stat];
				if (statFactor ~= nil) then
					itemScore = itemScore + math.floor(value * statFactor);
					itemScoreLog = itemScoreLog.." ".._G[stat];
				end
			end
			itemScore = itemScore + math.ceil(value);
		end
	end
	
	if (itemType ~= nil) then	
		itemScore = itemScore + string.len(itemType);
	end
	if (itemSubType ~= nil) then
		itemScore = itemScore + string.len(itemSubType);
	end
	
	-- and last we adjust for item level, so that higher level items always produce higher scores except for lower level items with exceptional stats
	itemScore = itemScore + (((itemQuality + 1) * 100) + (itemLevel * 12));
	
	local doNotUseList = Mubox.Resources.ClassDoNotUse[Mubox.Player.Class];
	if (doNotUseList ~= nil) then
		for _,doNotUse in ipairs(doNotUseList) do
			if ((itemType == doNotUse) or (itemSubType == doNotUse)) then
				itemScore = -1 * itemScore;
			end
		end
	end

	if (log) then
		itemScoreLog = strtrim(itemScoreLog);
		if (string.len(itemScoreLog) > 0) then
			Mubox.Player.Write(itemLink.." "..Mubox.Resources.Colors.TextHighlight..itemScore..Mubox.Resources.Colors.Text.." ("..itemScoreLog..")");
		else
			Mubox.Player.Write(itemLink.." "..itemScore);
		end
	end
	
	return itemScore;
end 
