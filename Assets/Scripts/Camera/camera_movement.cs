using UnityEngine;

public class camera_movement : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float scrollSpeed = 1000f;
    public float minY = 10f;
    public float maxY = 80f;

    public float borderThickness = 10f; // Pixels from screen edge
    // public Vector2 mapLimitX = new Vector2(-50, 50);
    // public Vector2 mapLimitZ = new Vector2(-50, 50);

    void Update()
    {
        Vector3 pos = transform.position;

        // // Keyboard movement
        // if (Input.GetKey("w") || Input.mousePosition.y >= Screen.height - borderThickness)
        //     pos += Vector3.forward * moveSpeed * Time.deltaTime;
        // if (Input.GetKey("s") || Input.mousePosition.y <= borderThickness)
        //     pos += Vector3.back * moveSpeed * Time.deltaTime;
        // if (Input.GetKey("a") || Input.mousePosition.x <= borderThickness)
        //     pos += Vector3.left * moveSpeed * Time.deltaTime;
        // if (Input.GetKey("d") || Input.mousePosition.x >= Screen.width - borderThickness)
        //     pos += Vector3.right * moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.W))
            pos += Vector3.forward * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            pos += Vector3.back * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A))
            pos += Vector3.left * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            pos += Vector3.right * moveSpeed * Time.deltaTime;

        // Zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        pos.y -= scroll * scrollSpeed * Time.deltaTime;

        // Clamp zoom and movement
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        // pos.x = Mathf.Clamp(pos.x, mapLimitX.x, mapLimitX.y);
        // pos.z = Mathf.Clamp(pos.z, mapLimitZ.x, mapLimitZ.y);

        transform.position = pos;
    }
}
