using UnityEngine;

public class LayerInfo
{
    public static int Ground = LayerMask.NameToLayer("Ground");
    public static int Obstacle = LayerMask.NameToLayer("Obstacle");
    public static int CreatureDef = LayerMask.NameToLayer("CreatureDef");
    public static int CreatureAtt = LayerMask.NameToLayer("CreatureAtt");
    public static int Drop = LayerMask.NameToLayer("Drop");

    public static int Interaction = LayerMask.NameToLayer("Interaction");
    public static int Other = LayerMask.NameToLayer("Other");
}
