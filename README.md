# voice-attack-osc-plugin
Trigger commands via OSC and Send OSC messages

Disclaimer: There is no handling for if the OSC receiver disconnects for some reason. So far it has not been a problem for me.

# How to Trigger Voice Attack commands via OSC
- Extract plugin to Apps folder in voiceattack program files folder
- Edit oscsettings.txt in the plugin folder, and add your OSC trigger address to command mapping in the following format:

`{OscAddress};{VoiceAttackCommandName}`

For example, if you'd like voice attack to trigger the "calculator" command when you hit the osc address of `/va/calculator`:

`/va/calculator;calculator`

# How to send OSC from VoiceAttack
- Use Other > Advanced > Execute an External Plugin Function
- In the plugin context put the Osc Address you'd like to send a message to
- If you'd like to send any params with data type of integers, floats, booleans, or strings, separate each param with a semicolon after a colon

`{OscAddressToSendTo}:{ParamList}`

For example, if you'd like to send an integer of 3 to the `/chataigne/inputselector` address, you'd put the following into the plugin context:

`/chataigne/inputselector:i 2`

You can also send multiple params separated with a semicolon

`/chataigne/inputselector:i 2;s cool stuff`

If you just want to trigger osc and not send any params, do not specify a param list

`/chataigne/fx1on`
