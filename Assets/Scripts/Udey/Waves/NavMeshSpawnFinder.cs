using UnityEngine;
using UnityEngine.AI;


/// Finds somewhere to put an enemy: a random point on the NavMesh, in a ring around the
/// player, that the enemy can actually walk from, and ideally not somewhere the player is
/// currently looking.

/// Everything is static — there's nothing to put in the scene.

public static class NavMeshSpawnFinder
{
    // Reused so repeated searches don't allocate a path object per attempt.
    private static readonly NavMeshPath ReusablePath = new NavMeshPath();


    /// Tries to find a spawn point around <paramref name="around"/>.

    /// <param name="around">Usually the player's position.</param>
    /// <param name="minDistance">Never spawn closer than this.</param>
    /// <param name="maxDistance">Never spawn further than this.</param>
    /// <param name="result">The point found, on the NavMesh.</param>
    /// <param name="attempts">How many random points to try before giving up.</param>
    /// <param name="sampleRadius">How far a random point may be nudged to land on the NavMesh.</param>
    /// <param name="avoidCamera">If set, rejects points visible to this camera.</param>
    /// <param name="sightBlockers">Layers that count as cover for the visibility test.</param>
    /// <param name="requireReachable">Reject points with no complete path back to the player.</param>
    public static bool TryFindSpawnPoint(
        Vector3 around,
        float minDistance,
        float maxDistance,
        out Vector3 result,
        int attempts = 30,
        float sampleRadius = 4f,
        Camera avoidCamera = null,
        LayerMask sightBlockers = default,
        bool requireReachable = true)
    {
        result = around;

        if (maxDistance < minDistance) (minDistance, maxDistance) = (maxDistance, minDistance);

        float minSqr = minDistance * minDistance;

        for (int i = 0; i < attempts; i++)
        {
            // Uniform across the ring's AREA — without the sqrt, points bunch up near the inner edge.
            float angle = Random.value * Mathf.PI * 2f;
            float radius = Mathf.Sqrt(Random.Range(minSqr, maxDistance * maxDistance));

            Vector3 candidate = around + new Vector3(
                Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                continue;
            }

            // SamplePosition can drag the point back toward the player — re-check the gap.
            Vector3 flatDelta = hit.position - around;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < minSqr) continue;

            if (avoidCamera != null && IsVisibleTo(avoidCamera, hit.position, sightBlockers)) continue;

            if (requireReachable && !IsReachable(hit.position, around)) continue;

            result = hit.position;
            return true;
        }

        return false;
    }

    /// True when the point is inside the camera frustum and not hidden behind cover.
    public static bool IsVisibleTo(Camera camera, Vector3 worldPoint, LayerMask sightBlockers)
    {
        // Lift it to roughly chest height — a point on the floor can be off-screen while the
        // enemy standing on it is in full view.
        Vector3 checkPoint = worldPoint + Vector3.up * 1f;

        Vector3 viewport = camera.WorldToViewportPoint(checkPoint);
        bool inFrustum = viewport.z > 0f
                         && viewport.x > -0.05f && viewport.x < 1.05f
                         && viewport.y > -0.05f && viewport.y < 1.05f;

        if (!inFrustum) return false;

        // In the frustum but behind a wall still counts as hidden.
        if (sightBlockers.value != 0)
        {
            Vector3 cameraPosition = camera.transform.position;
            if (Physics.Linecast(cameraPosition, checkPoint, sightBlockers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
        }

        return true;
    }

    /// True when a complete NavMesh path exists between the two points.
    public static bool IsReachable(Vector3 from, Vector3 to)
    {
        if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, ReusablePath)) return false;
        return ReusablePath.status == NavMeshPathStatus.PathComplete;
    }
}
