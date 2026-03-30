using UnityEngine;

public class LayerInfo
{
    public static int Ground = LayerMask.NameToLayer("Ground");
    public static int Obstacle = LayerMask.NameToLayer("Obstacle");

    public static int CreatureDef = LayerMask.NameToLayer("CreatureDef");
    public static int CreatureDef_Front = LayerMask.NameToLayer("CreatureDef_Front");
    public static int CreatureDef_Back = LayerMask.NameToLayer("CreatureDef_Back");

    public static int CreatureAtt = LayerMask.NameToLayer("CreatureAtt");
    public static int CreatureAtt_Front = LayerMask.NameToLayer("CreatureAtt_Front");
    public static int CreatureAtt_Back = LayerMask.NameToLayer("CreatureAtt_Back");

    public static int Drop = LayerMask.NameToLayer("Drop");

    public static int Interaction = LayerMask.NameToLayer("Interaction");
    public static int Other = LayerMask.NameToLayer("Other");
}
