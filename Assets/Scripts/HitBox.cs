using UnityEngine;

public enum HitBoxType { Head, Body, Legs }

public class HitBox : MonoBehaviour
{
    public HitBoxType type;
    public NetworkPlayer owner;
}
