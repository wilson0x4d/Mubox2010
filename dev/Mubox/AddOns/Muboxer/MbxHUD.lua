function Mubox.HUD.Write(message)
	if (message ~= nil) then
		UIErrorsFrame:AddMessage(message, 1.0, 1.0, 0.0, 1.0, UIERRORS_HOLD_TIME);
	end
end
