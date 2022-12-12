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

Program(){Runtime.UpdateFrequency=UpdateFrequency.Update1;}bool l,k,j=true,i=false,m=false;double o,Å,Æ,Ä;const double Ã
=360;IMyShipController Â;IMyMotorStator Á,À;int º;Vector3D µ,ª,w,v,u,t;Vector3D s=new Vector3D(0,0,0);const string r=
"Agneovo's 2 plane gun stabilizer script \nrunning",q=" with the name ",p="` is missing.";void Main(string P){if(l){v=(µ==s)?Â.WorldMatrix.Forward:µ;u=(ª==s)?Â.WorldMatrix
.Right:ª;t=(w==s)?Â.WorldMatrix.Up:w;µ=Â.WorldMatrix.Forward;ª=Â.WorldMatrix.Right;w=Â.WorldMatrix.Up;double f=G(v,µ);
double N=G(u,ª);double M=G(t,w);Æ+=((Math.Cos(Á.Angle)*(f+M-N)+Math.Sin(Á.Angle)*(N+M-f))/2)*(G(µ,t)<G(v,t)?1:-1);Ä+=(f+N-M)/2
*(G(v,ª)>G(v,u)?1:-1);double L=Â.RotationIndicator.Y*sensitivity;double K=Â.RotationIndicator.X*sensitivity;Å+=L;o-=K;Z(À
,(-Æ+o),-limitDown,limitUp);Y(Á,(-Ä+Å),-limitLeft,limitRight);º++;if(º>80){º=0;}if(º<20){Echo(r);}else if(º<40){Echo(r+
" .");if(j){j=false;Á.ApplyAction("OnOff_On");À.ApplyAction("OnOff_On");}}else if(º<60){Echo(r+" ..");}else if(º<80){Echo(r+
" ...");}}else{k=false;Â=(IMyShipController)S(CockpitName);Á=(IMyMotorStator)S(HorizName);À=(IMyMotorStator)S(VertName);if(Â==
null){Echo("Cockpit"+q+"`"+CockpitName+p);}if(À==null){Echo("Rotor"+q+"`"+HorizName+p);}if(Á==null){Echo("Rotor"+q+"`"+
VertName+p);}if(!k){l=true;j=true;Á.ApplyAction("OnOff_Off");À.ApplyAction("OnOff_Off");}º=0;}}double J(double I){return O(Math.
Acos(I));}double O(double H){return H*(180.0/Math.PI);}double F(double D){if(D>horizontalSpeedLimit){return
horizontalSpeedLimit;}if(D<-horizontalSpeedLimit){return-horizontalSpeedLimit;}return D;}double E(double D){if(D>verticalSpeedLimit){return
verticalSpeedLimit;}if(D<-verticalSpeedLimit){return-verticalSpeedLimit;}return D;}double C(double B){if(B<0){return-B;}return B;}double G
(Vector3D A,Vector3D Q){if(A==Q){return 0;}double g=(A.X*Q.X)+(A.Y*Q.Y)+(A.Z*Q.Z);double e=Math.Sqrt(A.X*A.X+A.Y*A.Y+A.Z*
A.Z);double c=Math.Sqrt(Q.X*Q.X+Q.Y*Q.Y+Q.Z*Q.Z);return J(g/(e*c));}void Z(IMyMotorStator X,double W,double V,double U){
double T=O(X.Angle);if(W>U){X.SetValueFloat("LowerLimit",(float)U);X.SetValueFloat("UpperLimit",(float)U);}else if(W<V){X.
SetValueFloat("LowerLimit",(float)V);X.SetValueFloat("UpperLimit",(float)V);}else if(W>T){X.SetValueFloat("LowerLimit",(float)V);X.
SetValueFloat("UpperLimit",(float)W);}else{X.SetValueFloat("LowerLimit",(float)W);X.SetValueFloat("UpperLimit",(float)U);}X.
SetValueFloat("Velocity",(float)E((W-T)*6f));}void Y(IMyMotorStator X,double W,double V,double U){double T=O(X.Angle);if(W>360||i){T
%=Ã;X.SetValueFloat("Velocity",(float)F((Ã-T+(W%Ã))*6f));X.SetValueFloat("LowerLimit",float.MinValue);X.SetValueFloat(
"UpperLimit",float.MaxValue);if(!i){Ä+=Ã;}i=true;if(T<60){i=false;}return;}if(W<-360||m){T=-(T%Ã);X.SetValueFloat("Velocity",(float)
F((T-Ã-(W%Ã))*6f));X.SetValueFloat("LowerLimit",float.MinValue);X.SetValueFloat("UpperLimit",float.MaxValue);if(!m){Ä-=Ã;
}m=true;if(T>-60){m=false;}return;}if(W>=U){X.SetValueFloat("LowerLimit",(float)0);X.SetValueFloat("UpperLimit",(float)0)
;}else if(W<=V){X.SetValueFloat("LowerLimit",(float)-0);X.SetValueFloat("UpperLimit",(float)-0);}else if(W>T){X.
SetValueFloat("LowerLimit",(float)V);X.SetValueFloat("UpperLimit",(float)W);}else{X.SetValueFloat("LowerLimit",(float)W);X.
SetValueFloat("UpperLimit",(float)U);}X.SetValueFloat("Velocity",(float)F((W-T)*6f));}IMyTerminalBlock S(string h){IMyTerminalBlock R
=GridTerminalSystem.GetBlockWithName(h);if(R==null){k=true;}return R;}