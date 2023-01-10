///////////////////////////////////////////////////////
//  Based on Xionphs stabilized mouse-turret script  //
///////////////////////////////////////////////////////
//            CUSTOMIZABLE VARIABLES:                //
///////////////////////////////////////////////////////

// This script works with rotor, advanced rotor, and hinge
// You can find the not-minified version on my github:
// https://github.com/agneovo123/space-engineers-scripts/blob/main/SpaceEngineersScriptingProject/2-plane-gun-stabilization/Program.cs

// sets your sensitivity
const double sensitivity = 0.05;
// how fast the horizontal rotor can go value: 0 to 60
const double horizontalSpeedLimit = 30;
// how fast the vertical rotor can go value: 0 to 60
const double verticalSpeedLimit = 30;
// the next 4 variables range from -360 to 360
// how much the turret can turn left (set to -360 to make it unlimited)
const double limitLeft = -360;
// how much the turret can turn right (set to 360 to make it unlimited)
const double limitRight = 360;
// how much the turret can turn up
const double limitUp = 40;
// how much the turret can turn down
const double limitDown = -12;

// the name of your regular/industrial/rover/buggy cockpit
const string CockpitName = "aTankDriver";
// the name of your horizontal (left-right) rotor
const string HorizName = "Rotor Horizontal";
// the name of your vertical (up-down) rotor
const string VertName = "Rotor Vertical";
////////////////////////////////////////////////////////////////////////////////
//                 DO NOT EDIT ANYTHING BELOW THIS LINE                       //
////////////////////////////////////////////////////////////////////////////////
public Program(){Runtime.UpdateFrequency = UpdateFrequency.Update1; }
bool d,e,f=true,g=false,h=false,µ=true;double w,v,u,t,s,r;const double ª=360,q=(180.0/Math.PI);IMyShipController o;
IMyMotorStator n,m;int l;Vector3D k,j,i,c,A,N;Vector3D B=new Vector3D(0,0,0);MyCommandLine C=new MyCommandLine();void Main(string D){
if(Me.CustomData!=null){C.TryParse(Me.CustomData.Replace(";"," "));}else if(µ){C.TryParse("");}if(d&&!C.Switch("resetGun")
){c=(k==B)?o.WorldMatrix.Forward:k;A=(j==B)?o.WorldMatrix.Right:j;N=(i==B)?o.WorldMatrix.Up:i;k=o.WorldMatrix.Forward;j=o
.WorldMatrix.Right;i=o.WorldMatrix.Up;double E=K(c,k);double F=K(A,j);double G=K(N,i);s+=((Math.Cos(n.Angle)*(E+G-F)+Math
.Sin(n.Angle)*(F+G-E))/2)*(K(k,N)<K(c,N)?1:-1);r+=(E+F-G)/2*(K(c,j)>K(c,A)?1:-1);v=o.RotationIndicator.Y*sensitivity;w=o.
RotationIndicator.X*sensitivity;t+=v;u-=w;Q(m,(-s+u),limitDown,limitUp);W(n,(-r+t),limitLeft,limitRight);l++;if(l>80){l=0;}if(l<20){Echo(
"Agneovo's 2 plane gun stabilizer script \nrunning");}else if(l<40){Echo("Agneovo's 2 plane gun stabilizer script \nrunning.");if(f){f=false;n.ApplyAction("OnOff_On");m.
ApplyAction("OnOff_On");}}else if(l<60){Echo("Agneovo's 2 plane gun stabilizer script \nrunning..");}else if(l<80){Echo(
"Agneovo's 2 plane gun stabilizer script \nrunning...");}}else{e=false;o=(IMyShipController)X(CockpitName);n=(IMyMotorStator)X(HorizName);m=(IMyMotorStator)X(VertName);if(o==
null){Echo("Cockpit with the name `"+CockpitName+"` is missing.");}if(m==null){Echo("Rotor with the name `"+HorizName+
"` is missing.");}if(n==null){Echo("Rotor with the name `"+VertName+"` is missing.");}if(limitUp<-360||limitUp>360){Echo(
"limitUp must be between -360 and 360");}if(limitDown<-360||limitDown>360){Echo("limitDown must be between -360 and 360");}if(limitLeft<-360||limitLeft>360){
Echo("limitLeft must be between -360 and 360");}if(limitRight<-360||limitRight>360){Echo(
"limitRight must be between -360 and 360");}if(!e){d=true;n.ApplyAction("OnOff_Off");m.ApplyAction("OnOff_Off");f=true;w=v=u=t=s=r=0;Me.CustomData="";}l=0;}}
double H(double I){if(I>horizontalSpeedLimit){return horizontalSpeedLimit;}if(I<-horizontalSpeedLimit){return-
horizontalSpeedLimit;}return I;}double J(double I){if(I>verticalSpeedLimit){return verticalSpeedLimit;}if(I<-verticalSpeedLimit){return-
verticalSpeedLimit;}return I;}double K(Vector3D L,Vector3D M){if(L==M){return 0;}double O=(L.X*M.X)+(L.Y*M.Y)+(L.Z*M.Z);double Y=Math.Sqrt
(L.X*L.X+L.Y*L.Y+L.Z*L.Z);double P=Math.Sqrt(M.X*M.X+M.Y*M.Y+M.Z*M.Z);return Math.Acos(O/(Y*P))*q;}void Q(IMyMotorStator
R,double S,double T,double U){double V=R.Angle*q;if(S>U){R.SetValueFloat("LowerLimit",(float)U);R.SetValueFloat(
"UpperLimit",(float)U);}else if(S<T){R.SetValueFloat("LowerLimit",(float)T);R.SetValueFloat("UpperLimit",(float)T);}else if(S>V){R.
SetValueFloat("LowerLimit",(float)T);R.SetValueFloat("UpperLimit",(float)S);}else{R.SetValueFloat("LowerLimit",(float)S);R.
SetValueFloat("UpperLimit",(float)U);}R.SetValueFloat("Velocity",(float)J((S-V)*6f));}void W(IMyMotorStator R,double S,double T,
double U){double V=R.Angle*q;if(S>360||g){V%=ª;R.SetValueFloat("Velocity",(float)H((ª-V+(S%ª))*6f));R.SetValueFloat(
"LowerLimit",float.MinValue);R.SetValueFloat("UpperLimit",float.MaxValue);if(!g){r+=ª;}g=true;if(V<60){g=false;}return;}if(S<-360||h
){V=-(V%ª);R.SetValueFloat("Velocity",(float)H((V-ª-(S%ª))*6f));R.SetValueFloat("LowerLimit",float.MinValue);R.
SetValueFloat("UpperLimit",float.MaxValue);if(!h){r-=ª;}h=true;if(V>-60){h=false;}return;}if(S>=U){R.SetValueFloat("LowerLimit",(
float)0);R.SetValueFloat("UpperLimit",(float)0);}else if(S<=T){R.SetValueFloat("LowerLimit",(float)-0);R.SetValueFloat(
"UpperLimit",(float)-0);}else if(S>V){R.SetValueFloat("LowerLimit",(float)T);R.SetValueFloat("UpperLimit",(float)S);}else{R.
SetValueFloat("LowerLimit",(float)S);R.SetValueFloat("UpperLimit",(float)U);}R.SetValueFloat("Velocity",(float)H((S-V)*6f));}
IMyTerminalBlock X(string p){IMyTerminalBlock Z=GridTerminalSystem.GetBlockWithName(p);if(Z==null){e=true;}return Z;}