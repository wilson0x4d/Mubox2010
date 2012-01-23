function Mubox.Equipment.Repair()
	local noRepairNecessary = true;
	-- custom repair of equipment, and not inventory, inventory slots are ordered by repair priority (e.g. chest before feet)
	local playerMoney = GetMoney();
	local shouldResetCursor = false;
	if (CanMerchantRepair()) then
		if (InRepairMode() == nil) then
			ShowRepairCursor();
			shouldResetCursor = true;
		end
		if (InRepairMode()) then
			for _,v in pairs(Mubox.Equipment.SlotNames) do
				local inventoryId = GetInventorySlotInfo(v);
				local itemId = GetInventoryItemID("player", inventoryId);
				if (itemId ~= nil) then
					local name, link, quality, iLevel, reqLevel, class, subclass, maxStack, equipSlot, texture, vendorPrice = GetItemInfo(itemId);
					if (link ~= nill) then

						-- something between patch 33 and 401 got fucked, so now we repair regardless of durability, because it's all fucked now. fucking bullshit.
						PickupInventoryItem(inventoryId);

						-- the following code is the original code and, who knows, maybe blizz will de-fuck-it on their own

						local durability,max  = GetInventoryItemDurability(inventoryId);
						if (durability ~= max) then
							local estimatedRepairCost = 0.010;
							if (quality <= 1) then -- common
								estimatedRepairCost = 0.010;
							elseif (quality == 2) then -- uncommon
								estimatedRepairCost = 0.020;
							elseif (quality == 3) then -- rare
								estimatedRepairCost = 0.025;
							elseif (quality >= 4) then -- epic
								estimatedRepairCost = 0.050;
							end

							-- TODO determine/implement legendary and trash repair costs
							-- TODO verify repair costs for weapons, armor, and shields/etc
							-- TODO implement the above as a table out of MbxCore, instead of hard-coded values

							estimatedRepairCost = (estimatedRepairCost * (max - durability) * (iLevel - 32.5)) * 100;
							
							-- TODO determine/implement faction discount
							
							if (estimatedRepairCost > 0) then
								noRepairNecessary = false;
								if (playerMoney >= estimatedRepairCost) then
									PickupInventoryItem(inventoryId);
									Mubox.Player.Write("Repaired "..link.." for "..Mubox.Util.ConvertMoneyToString(estimatedRepairCost));
									playerMoney = playerMoney - estimatedRepairCost;
								end
							end
						end
					end
				end
			end
		end
		if (shouldResetCursor) then
			HideRepairCursor();
		end
		if (noRepairNecessary) then
			Mubox.Player.Write("No repairs were necessary. (this message was broken in patch 401 and may be incorrect)");
		end
	end
	Mubox.Persistence.Player.Money = GetMoney();
end
