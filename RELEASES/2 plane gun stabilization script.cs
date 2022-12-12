///////////////////////////////////////////////////////
//  Based on Xionphs stabilized mouse-turret script  //
///////////////////////////////////////////////////////
//              EDITABLE VARIABLES:                  //
///////////////////////////////////////////////////////

// This script works with rotor, advanced rotor, and hinge
// You can find the not-minified version on my github:
// https://github.com/agneovo123/space-engineers-scripts/blob/main/SpaceEngineersScriptingProject/2-plane-gun-stabilization/Program.cs

// sets your sensitivity
const double sensitivity = 0.05;
// how fast the horizontal rotor can go value: 0 to 60
const double horizontalSpeedLimit = 60;
// how fast the vertical rotor can go value: 0 to 60
const double verticalSpeedLimit = 60;
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

Program(){Runtime.UpdateFrequency=UpdateFrequency.Update1;}bool l,k,j=true,i=false,m=false;double o,Å,Æ,Ä,Ã,Â;const
double Á=360;IMyShipController À;IMyMotorStator º,µ;int ª;Vector3D w,v,u,t,s,r;Vector3D q=new Vector3D(0,0,0);const string p=
"Agneovo's 2 plane gun stabilizer script \nrunning",g=" with the name ",P="` is missing.";void Main(string N){if(l){t=(w==q)?À.WorldMatrix.Forward:w;s=(v==q)?À.WorldMatrix
.Right:v;r=(u==q)?À.WorldMatrix.Up:u;w=À.WorldMatrix.Forward;v=À.WorldMatrix.Right;u=À.WorldMatrix.Up;double M=G(t,w);
double L=G(s,v);double K=G(r,u);Ã+=((Math.Cos(º.Angle)*(M+K-L)+Math.Sin(º.Angle)*(L+K-M))/2)*(G(w,r)<G(t,r)?1:-1);Â+=(M+L-K)/2
*(G(t,v)>G(t,s)?1:-1);Å=À.RotationIndicator.Y*sensitivity;o=À.RotationIndicator.X*sensitivity;Ä+=Å;Æ-=o;Z(µ,(-Ã+Æ),-
limitDown,limitUp);Y(º,(-Â+Ä),-limitLeft,limitRight);ª++;if(ª>80){ª=0;}if(ª<20){Echo(p);}else if(ª<40){Echo(p+" .");if(j){j=false
;º.ApplyAction("OnOff_On");µ.ApplyAction("OnOff_On");}}else if(ª<60){Echo(p+" ..");}else if(ª<80){Echo(p+" ...");}}else{k
=false;À=(IMyShipController)S(CockpitName);º=(IMyMotorStator)S(HorizName);µ=(IMyMotorStator)S(VertName);if(À==null){Echo(
"Cockpit"+g+"`"+CockpitName+P);}if(µ==null){Echo("Rotor"+g+"`"+HorizName+P);}if(º==null){Echo("Rotor"+g+"`"+VertName+P);}if(!k){l
=true;j=true;º.ApplyAction("OnOff_Off");µ.ApplyAction("OnOff_Off");}ª=0;}}double J(double I){return O(Math.Acos(I));}
double O(double H){return H*(180.0/Math.PI);}double F(double D){if(D>horizontalSpeedLimit){return horizontalSpeedLimit;}if(D<-
horizontalSpeedLimit){return-horizontalSpeedLimit;}return D;}double E(double D){if(D>verticalSpeedLimit){return verticalSpeedLimit;}if(D<-
verticalSpeedLimit){return-verticalSpeedLimit;}return D;}double C(double B){if(B<0){return-B;}return B;}double G(Vector3D A,Vector3D Q){if
(A==Q){return 0;}double f=(A.X*Q.X)+(A.Y*Q.Y)+(A.Z*Q.Z);double e=Math.Sqrt(A.X*A.X+A.Y*A.Y+A.Z*A.Z);double c=Math.Sqrt(Q.
X*Q.X+Q.Y*Q.Y+Q.Z*Q.Z);return J(f/(e*c));}void Z(IMyMotorStator X,double W,double V,double U){double T=O(X.Angle);if(W>U)
{Æ+=o;X.SetValueFloat("LowerLimit",(float)U);X.SetValueFloat("UpperLimit",(float)U);}else if(W<V){Æ+=o;X.SetValueFloat(
"LowerLimit",(float)V);X.SetValueFloat("UpperLimit",(float)V);}else if(W>T){X.SetValueFloat("LowerLimit",(float)V);X.SetValueFloat(
"UpperLimit",(float)W);}else{X.SetValueFloat("LowerLimit",(float)W);X.SetValueFloat("UpperLimit",(float)U);}X.SetValueFloat(
"Velocity",(float)E((W-T)*6f));}void Y(IMyMotorStator X,double W,double V,double U){double T=O(X.Angle);if(W>360||i){T%=Á;X.
SetValueFloat("Velocity",(float)F((Á-T+(W%Á))*6f));X.SetValueFloat("LowerLimit",float.MinValue);X.SetValueFloat("UpperLimit",float.
MaxValue);if(!i){Â+=Á;}i=true;if(T<60){i=false;}return;}if(W<-360||m){T=-(T%Á);X.SetValueFloat("Velocity",(float)F((T-Á-(W%Á))*
6f));X.SetValueFloat("LowerLimit",float.MinValue);X.SetValueFloat("UpperLimit",float.MaxValue);if(!m){Â-=Á;}m=true;if(T>-
60){m=false;}return;}if(W>=U){X.SetValueFloat("LowerLimit",(float)0);X.SetValueFloat("UpperLimit",(float)0);}else if(W<=V)
{X.SetValueFloat("LowerLimit",(float)-0);X.SetValueFloat("UpperLimit",(float)-0);}else if(W>T){X.SetValueFloat(
"LowerLimit",(float)V);X.SetValueFloat("UpperLimit",(float)W);}else{X.SetValueFloat("LowerLimit",(float)W);X.SetValueFloat(
"UpperLimit",(float)U);}X.SetValueFloat("Velocity",(float)F((W-T)*6f));}IMyTerminalBlock S(string h){IMyTerminalBlock R=
GridTerminalSystem.GetBlockWithName(h);if(R==null){k=true;}return R;}