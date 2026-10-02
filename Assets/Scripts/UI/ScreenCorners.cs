using UnityEngine;

// The places of the layout that stick to the corners and edges of the screen (see ScreenCorner). It sits on the layout itself.
public class ScreenCorners : MonoBehaviour
{
    [SerializeField] private Transform _topLeft;
    [SerializeField] private Transform _topRight;

    public Transform TopLeft => _topLeft;
    public Transform TopRight => _topRight;

    // The controls of the player who has the turn sit in his top corner: left for Red Boss, right for Blue Boss.
    public Transform TurnSide(int playerId) => playerId == 1 ? _topLeft : _topRight;
}
