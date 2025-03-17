using System.Collections;
using UnityEngine;

public class MovingPlatformBehavior : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float waitTime = 3f;
    public float[] stopHeights; // List of Y positions for stops

    private int currentStopIndex = 0;
    private int direction = 1; // 1 = Up, -1 = Down

    private void Start()
    {
        if (stopHeights.Length < 2)
        {
            Debug.LogError("You need at least 2 stops for the platform to work!");
            return;
        }

        StartCoroutine(MovePlatform());
    }

    private IEnumerator MovePlatform()
    {
        while (true)
        {
            // Get the target Y position
            Vector3 targetPos = transform.position;
            targetPos.y = stopHeights[currentStopIndex];

            // Move to the next stop
            yield return StartCoroutine(MoveToPosition(targetPos));

            // Wait at the stop
            yield return new WaitForSeconds(waitTime);

            // Update stop index
            currentStopIndex += direction;

            // Reverse direction at the top or bottom
            if (currentStopIndex == stopHeights.Length - 1 || currentStopIndex == 0)
            {
                direction *= -1;
            }
        }
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
