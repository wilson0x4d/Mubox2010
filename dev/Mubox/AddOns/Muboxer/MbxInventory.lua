function Mubox.Inventory.AutoVendor()
	local noSalesPossible = true;
	if (Mubox.Persistence.MaxQualityAutoVendor ~= nil) then
		local playerBeginMoney = GetMoney();
		-- Adapted from "SellGrays" AddOn
		for x = 0, 4 do 
			for y = 1, GetContainerNumSlots(x) do
				local link = GetContainerItemLink(x,y);
				if (link) then
					local name, link, quality, itemLevel, itemMinLevel, itemType, itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = 
						GetItemInfo(link);
					local vendorOkay = (quality <= Mubox.Persistence.MaxQualityAutoVendor);
					if (Mubox.Persistence.MaxItemLevelAutoVendor ~= nil) then
						if (itemLevel > Mubox.Persistence.MaxItemLevelAutoVendor) then
							vendorOkay = false;
						end
					end
				
					for _,itemTypeNoSales in pairs(Mubox.Resources.VendorDoNotSell) do
						if ((itemType == itemTypeNoSales) or (itemSubType == itemTypeNoSales)) then
							vendorOkay = false;
						end 
					end

					if (vendorOkay) then
						-- TODO do not sell soulbound items
						-- TODO do not sell instance loot with an outstanding trade duration
						-- TODO do not sell crafting items for own professions
						PickupContainerItem(x,y);
						PickupMerchantItem();
						Mubox.Player.Write("Sold "..link);
						noSalesPossible = false;
					end
				end		
			end
		end
		local playerEndMoney = GetMoney();
		if (playerBeginMoney < playerEndMoney) then
			Mubox.Player.Write("Sales Total: "..Mubox.Util.ConvertMoneyToString(playerEndMoney - playerBeginMoney));
		end	
	end
	Mubox.Persistence.Player.Money = GetMoney();
	if (noSalesPossible) then
		Mubox.Player.Write("Nothing was sold.");
	else
		Mubox.Inventory.Bags.ShouldSort = true;
	end
end

function Mubox.Inventory.Bags.Sort(resumeBagId, resumeSlotId)
	if (not resumeBagId) then
		resumeBagId = 4;
	end
	if (not resumeSlotId) then
		resumeSlotId = 1;
	end

	if (IsShiftKeyDown()) then
		-- user short circuit
		Mubox.Inventory.Bags.ShouldSort = true;
		return;
	end

	-- if player has an item picked up, do not attempt to sort bags
	if (CursorHasItem()) then
		Mubox.Inventory.Bags.ShouldSort = true;
		return;
	end

	if (Mubox.Inventory.Bags.IsBusySorting) then
		Mubox.Inventory.Bags.ShouldSort = true;
		return;
	end
	Mubox.Inventory.Bags.IsBusySorting = true;

	if (not resumeBagId) then
		resumeBagId = 4;
	end
	if (not resumeSlotId) then
		resumeSlotId = 1;
	end

	Mubox.Inventory.Bags.ShouldSort = false;
	for leftBag = resumeBagId, 0, -1 do 
		local leftBagNumSlots = GetContainerNumSlots(leftBag);
		for leftSlot = resumeSlotId, leftBagNumSlots do
			-- resolve left and right targets
			local rightBag = leftBag;
			local rightSlot = leftSlot + 1;
			if (rightSlot > leftBagNumSlots) then
				rightBag = leftBag - 1;
				if (rightBag > -1) then
					rightSlot = 1;--GetContainerNumSlots(rightBag);
				end
			end
			local rightBagNumSlots = GetContainerNumSlots(rightBag);
			-- compare targets and swap if possible
			if (rightBag > -1) then
				local _, leftSlotCount, leftLocked, _, _, _, leftLink = GetContainerItemInfo(leftBag, leftSlot);
				if (not leftLocked and leftLink ~= nil) then
					local _, rightSlotCount, rightLocked, _, _, _, rightLink = GetContainerItemInfo(rightBag, rightSlot);
					if (rightLocked) then
						leftSlot = leftSlot + 1;
						Mubox.Inventory.Bags.ShouldSort = true;
					else
						-- derive scores
						if (leftSlotCount == nil) then
							leftSlotCount = 0;
						end
						local leftScore = Mubox.Util.GetItemSortKey(leftLink, nil, false) .. leftSlotCount;
						if (rightSlotCount == nil) then
							rightSlotCount = 0;
						end
						local rightScore = Mubox.Util.GetItemSortKey(rightLink, nil, false) .. rightSlotCount;
						
						-- apply derived scores
						if (leftScore < rightScore) then
							PickupContainerItem(leftBag, leftSlot);
							PickupContainerItem(rightBag, rightSlot);
							Mubox.Inventory.Bags.ShouldSort = true;
						elseif (rightScore > leftScore) then
							PickupContainerItem(rightBag, rightSlot);
							PickupContainerItem(leftBag, leftSlot);
							Mubox.Inventory.Bags.ShouldSort = true;
						else
							-- TODO: sort by?
						end
					end
				end
			end
		end
	end
	Mubox.Inventory.Bags.IsBusySorting = false;
end

