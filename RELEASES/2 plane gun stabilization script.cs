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
bool i,h,g=true,j=false,f=false,k=true;double Â,À,º,µ,ª,w;const double v=360,u=(180.0/Math.PI);IMyShipController t;
IMyMotorStator s,r;int q;Vector3D p,o,n,m,l,e;Vector3D O=new Vector3D(0,0,0);const string A=
"Agneovo's 2 plane gun stabilizer script \nrunning",M=" with the name ",L="` is missing.",K=" cannot be lover than 0";MyCommandLine J=new MyCommandLine();void Main(string
I){if(Me.CustomData!=null){J.TryParse(Me.CustomData.Replace(";"," "));}else if(k){J.TryParse("");}if(i&&!J.Switch(
"resetGun")){m=(p==O)?t.WorldMatrix.Forward:p;l=(o==O)?t.WorldMatrix.Right:o;e=(n==O)?t.WorldMatrix.Up:n;p=t.WorldMatrix.Forward;o
=t.WorldMatrix.Right;n=t.WorldMatrix.Up;double N=B(m,p);double H=B(l,o);double F=B(e,n);ª+=((Math.Cos(s.Angle)*(N+F-H)+
Math.Sin(s.Angle)*(H+F-N))/2)*(B(p,e)<B(m,e)?1:-1);w+=(N+H-F)/2*(B(m,o)>B(m,l)?1:-1);À=t.RotationIndicator.Y*sensitivity;Â=t
.RotationIndicator.X*sensitivity;µ+=À;º-=Â;Z(r,(-ª+º),-limitDown,limitUp);Y(s,(-w+µ),-limitLeft,limitRight);q++;if(q>80){
q=0;}if(q<20){Echo(A);}else if(q<40){Echo(A+" .");if(g){g=false;s.ApplyAction("OnOff_On");r.ApplyAction("OnOff_On");}}
else if(q<60){Echo(A+" ..");}else if(q<80){Echo(A+" ...");}}else{h=false;t=(IMyShipController)R(CockpitName);s=(
IMyMotorStator)R(HorizName);r=(IMyMotorStator)R(VertName);if(t==null){Echo("Cockpit"+M+"`"+CockpitName+L);}if(r==null){Echo("Rotor"+M+
"`"+HorizName+L);}if(s==null){Echo("Rotor"+M+"`"+VertName+L);}if(limitUp<0){Echo("limitUp"+K);}if(limitDown<0){Echo(
"limitDown"+K);}if(limitLeft<0){Echo("limitLeft"+K);}if(limitRight<0){Echo("limitRight"+K);}if(!h){i=true;s.ApplyAction("OnOff_Off"
);r.ApplyAction("OnOff_Off");g=true;Â=À=º=µ=ª=w=0;Me.CustomData="";}q=0;}}double E(double C){if(C>horizontalSpeedLimit){
return horizontalSpeedLimit;}if(C<-horizontalSpeedLimit){return-horizontalSpeedLimit;}return C;}double D(double C){if(C>
verticalSpeedLimit){return verticalSpeedLimit;}if(C<-verticalSpeedLimit){return-verticalSpeedLimit;}return C;}double B(Vector3D G,Vector3D
P){if(G==P){return 0;}double X=(G.X*P.X)+(G.Y*P.Y)+(G.Z*P.Z);double d=Math.Sqrt(G.X*G.X+G.Y*G.Y+G.Z*G.Z);double c=Math.
Sqrt(P.X*P.X+P.Y*P.Y+P.Z*P.Z);return Math.Acos(X/(d*c))*u;}void Z(IMyMotorStator W,double V,double U,double T){double S=W.
Angle*u;if(V>T){W.SetValueFloat("LowerLimit",(float)T);W.SetValueFloat("UpperLimit",(float)T);}else if(V<U){W.SetValueFloat(
"LowerLimit",(float)U);W.SetValueFloat("UpperLimit",(float)U);}else if(V>S){W.SetValueFloat("LowerLimit",(float)U);W.SetValueFloat(
"UpperLimit",(float)V);}else{W.SetValueFloat("LowerLimit",(float)V);W.SetValueFloat("UpperLimit",(float)T);}W.SetValueFloat(
"Velocity",(float)D((V-S)*6f));}void Y(IMyMotorStator W,double V,double U,double T){double S=W.Angle*u;if(V>360||j){S%=v;W.
SetValueFloat("Velocity",(float)E((v-S+(V%v))*6f));W.SetValueFloat("LowerLimit",float.MinValue);W.SetValueFloat("UpperLimit",float.
MaxValue);if(!j){w+=v;}j=true;if(S<60){j=false;}return;}if(V<-360||f){S=-(S%v);W.SetValueFloat("Velocity",(float)E((S-v-(V%v))*
6f));W.SetValueFloat("LowerLimit",float.MinValue);W.SetValueFloat("UpperLimit",float.MaxValue);if(!f){w-=v;}f=true;if(S>-
60){f=false;}return;}if(V>=T){W.SetValueFloat("LowerLimit",(float)0);W.SetValueFloat("UpperLimit",(float)0);}else if(V<=U)
{W.SetValueFloat("LowerLimit",(float)-0);W.SetValueFloat("UpperLimit",(float)-0);}else if(V>S){W.SetValueFloat(
"LowerLimit",(float)U);W.SetValueFloat("UpperLimit",(float)V);}else{W.SetValueFloat("LowerLimit",(float)V);W.SetValueFloat(
"UpperLimit",(float)T);}W.SetValueFloat("Velocity",(float)E((V-S)*6f));}IMyTerminalBlock R(string Á){IMyTerminalBlock Q=
GridTerminalSystem.GetBlockWithName(Á);if(Q==null){h=true;}return Q;}