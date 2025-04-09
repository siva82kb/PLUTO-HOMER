using UnityEngine;

public static class TrajectoryPredictor
{
    public static float PredictHitY(Vector2 initialPosition, Vector2 initialVelocity, float targetX, float topBound, float bottomBound, float bounceMultiplier)
    {
        // Make a copy so we can simulate without affecting the real ball.
        Vector2 pos = initialPosition;
        Vector2 vel = initialVelocity;

        // If the ball is already beyond the target, just return its current y.
        if (targetX <= pos.x)
            return pos.y;

        // Loop until the simulated ball reaches (or passes) the target x position.
        while (pos.x < targetX)
        {
            // Time required to travel in x until reaching the target.
            float tToTargetX = (targetX - pos.x) / vel.x;

            // Determine time to hit either the top or bottom bound.
            float tToWall = float.MaxValue;
            if (vel.y > 0)
            {
                tToWall = (topBound - pos.y) / vel.y;
            }
            else if (vel.y < 0)
            {
                tToWall = (bottomBound - pos.y) / vel.y;
            }

            // If the ball will hit a wall before reaching the target...
            if (tToWall >= 0 && tToWall < tToTargetX)
            {
                // Update position to the wall hit point.
                pos += vel * tToWall;
                // “Bounce” off the wall by reversing y and applying the multiplier.
                vel.y = -vel.y * bounceMultiplier;
                // (vx remains unchanged.)
            }
            else
            {
                // No wall collision before reaching target x:
                pos += vel * tToTargetX;
                // We now know the predicted y.
                return pos.y;
            }
        }

        return pos.y;
    }
}
