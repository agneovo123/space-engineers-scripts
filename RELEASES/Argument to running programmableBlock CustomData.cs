// Select your programmable block from a controller(cockpit/chair/etc)'s the G menu with the 'run' option.
// It will ask for arguments
// note: arguments are separated with spaces ' ', if your programmable block has has spaces in it's name, just put it in quotes example: "Programmable Block Gun stab"
// DO NOT use ';' in your arguments
// the first argument HAS TO BE the name of your programmable block
// the rest of the arguments will be written into your programmable block's customData field.
// to pass a flag/switch you write '-' before it, example: '"Programmable Block Gun stab" -resetGun'
// will put '-resetGun' in "Programmable Block Gun stab"'s CustomData
////////////////////////////////////////////////////////////////////////////////
//                 DO NOT EDIT ANYTHING BELOW THIS LINE                       //
////////////////////////////////////////////////////////////////////////////////
public Program(){}
MyCommandLine A=new MyCommandLine();void Main(string B,UpdateType C){if(A.TryParse(B)){if(A.Argument(0)==null){Echo(
"No argument! Refer to the beginning of the script for instructions.");}}if(B!=null&&B!=""){if(A.Argument(0).Contains(" ")){B=B.Substring(A.Argument(0).Length+3,B.Length-A.Argument(0).
Length-3);}}string D=A.Argument(0);string E="";for(int F=1;F<A.ArgumentCount;F++){E+=A.Argument(F);if(F+1<A.ArgumentCount){E+=
";";}}string[]G=A.Switches.ToArray();for(int F=0;F<G.Length;F++){if(F==0){E+=";";}E+="-"+G[F];if(F+1<G.Length){E+=";";}}
IMyProgrammableBlock H=(IMyProgrammableBlock)GridTerminalSystem.GetBlockWithName(D);if(H==null){Echo("Programmable block with name \'"+D+
"\' not found!");}else{H.CustomData=E;string I=H.CustomData;if(E==I){Echo("Arguments passed to \'"+D+"\': "+E);}else{Echo(
"Arguments unsuccessfully passed to \'"+D+"\'");}}}