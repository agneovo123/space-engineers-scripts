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
// how much the turret can turn left (set to 360 to make it unlimited)
const double limitLeft = 360;
// how much the turret can turn right (set to 360 to make it unlimited)
const double limitRight = 360;
// how much the turret can turn up (set to 360 to make it unlimited)
const double limitUp = 40;
// how much the turret can turn down (set to 360 to make it unlimited)
const double limitDown = 12;

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
bool h,g,f=true,i=false,j=false;double r,À,µ,ª,w,v;const double u=360,t=(180.0/Math.PI);IMyShipController º;
IMyMotorStator s,q;int p;Vector3D o,n,m,l,k,d;Vector3D A=new Vector3D(0,0,0);const string W=
"Agneovo's 2 plane gun stabilizer script \nrunning",B=" with the name ",C="` is missing.",D=" cannot be lover than 0";void Main(string E){if(h&&Me.CustomData!="reset"){l=(
o==A)?º.WorldMatrix.Forward:o;k=(n==A)?º.WorldMatrix.Right:n;d=(m==A)?º.WorldMatrix.Up:m;o=º.WorldMatrix.Forward;n=º.
WorldMatrix.Right;m=º.WorldMatrix.Up;double F=L(l,o);double G=L(k,n);double H=L(d,m);w+=((Math.Cos(s.Angle)*(F+H-G)+Math.Sin(s.
Angle)*(G+H-F))/2)*(L(o,d)<L(l,d)?1:-1);v+=(F+G-H)/2*(L(l,n)>L(l,k)?1:-1);À=º.RotationIndicator.Y*sensitivity;r=º.
RotationIndicator.X*sensitivity;ª+=À;µ-=r;Y(q,(-w+µ),-limitDown,limitUp);X(s,(-v+ª),-limitLeft,limitRight);p++;if(p>80){p=0;}if(p<20){
Echo(W);}else if(p<40){Echo(W+" .");if(f){f=false;s.ApplyAction("OnOff_On");q.ApplyAction("OnOff_On");}}else if(p<60){Echo(W
+" ..");}else if(p<80){Echo(W+" ...");}}else{g=false;º=(IMyShipController)Q(CockpitName);s=(IMyMotorStator)Q(HorizName);q
=(IMyMotorStator)Q(VertName);if(º==null){Echo("Cockpit"+B+"`"+CockpitName+C);}if(q==null){Echo("Rotor"+B+"`"+HorizName+C)
;}if(s==null){Echo("Rotor"+B+"`"+VertName+C);}if(limitUp<0){Echo("limitUp"+D);}if(limitDown<0){Echo("limitDown"+D);}if(
limitLeft<0){Echo("limitLeft"+D);}if(limitRight<0){Echo("limitRight"+D);}if(!g){h=true;s.ApplyAction("OnOff_Off");q.ApplyAction(
"OnOff_Off");f=true;r=À=µ=ª=w=v=0;Me.CustomData="";}p=0;}}double I(double J){if(J>horizontalSpeedLimit){return horizontalSpeedLimit
;}if(J<-horizontalSpeedLimit){return-horizontalSpeedLimit;}return J;}double K(double J){if(J>verticalSpeedLimit){return
verticalSpeedLimit;}if(J<-verticalSpeedLimit){return-verticalSpeedLimit;}return J;}double L(Vector3D M,Vector3D N){if(M==N){return 0;}
double O=(M.X*N.X)+(M.Y*N.Y)+(M.Z*N.Z);double c=Math.Sqrt(M.X*M.X+M.Y*M.Y+M.Z*M.Z);double Z=Math.Sqrt(N.X*N.X+N.Y*N.Y+N.Z*N.Z)
;return Math.Acos(O/(c*Z))*t;}void Y(IMyMotorStator V,double U,double T,double S){double R=V.Angle*t;if(U>S){V.
SetValueFloat("LowerLimit",(float)S);V.SetValueFloat("UpperLimit",(float)S);}else if(U<T){V.SetValueFloat("LowerLimit",(float)T);V.
SetValueFloat("UpperLimit",(float)T);}else if(U>R){V.SetValueFloat("LowerLimit",(float)T);V.SetValueFloat("UpperLimit",(float)U);}
else{V.SetValueFloat("LowerLimit",(float)U);V.SetValueFloat("UpperLimit",(float)S);}V.SetValueFloat("Velocity",(float)K((U-R
)*6f));}void X(IMyMotorStator V,double U,double T,double S){double R=V.Angle*t;if(U>360||i){R%=u;V.SetValueFloat(
"Velocity",(float)I((u-R+(U%u))*6f));V.SetValueFloat("LowerLimit",float.MinValue);V.SetValueFloat("UpperLimit",float.MaxValue);if(
!i){v+=u;}i=true;if(R<60){i=false;}return;}if(U<-360||j){R=-(R%u);V.SetValueFloat("Velocity",(float)I((R-u-(U%u))*6f));V.
SetValueFloat("LowerLimit",float.MinValue);V.SetValueFloat("UpperLimit",float.MaxValue);if(!j){v-=u;}j=true;if(R>-60){j=false;}return
;}if(U>=S){V.SetValueFloat("LowerLimit",(float)0);V.SetValueFloat("UpperLimit",(float)0);}else if(U<=T){V.SetValueFloat(
"LowerLimit",(float)-0);V.SetValueFloat("UpperLimit",(float)-0);}else if(U>R){V.SetValueFloat("LowerLimit",(float)T);V.SetValueFloat
("UpperLimit",(float)U);}else{V.SetValueFloat("LowerLimit",(float)U);V.SetValueFloat("UpperLimit",(float)S);}V.
SetValueFloat("Velocity",(float)I((U-R)*6f));}IMyTerminalBlock Q(string e){IMyTerminalBlock P=GridTerminalSystem.GetBlockWithName(e);
if(P==null){g=true;}return P;}