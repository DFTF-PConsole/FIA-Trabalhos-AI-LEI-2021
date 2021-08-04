using System;
using System.Collections;
using UnityEngine;


public class LinearRobotUnitBehaviour : RobotUnit
{
    public bool debugModePrint = true;


    // Resource
    private float resouceAngle;
    private float resourceValue;
    public float resourceWeight = 1f;                                       // Peso
    public int resourceMode = 1;                                            // Qual a funcao de ativacao (Mode = 1, 2, 3)?
    public float resourceLimiarMinX = 0f, resourceLimiarMaxX = 1f;          // Quais os Limiares (a < X < b)
    public float resourceLimiteMinY = 0f, resourceLimiteMaxY = 1f;          // Quais os Limites (a < Y < b)


    // Obstacle
    private float obstacleAngle;
    private float obstacleValue;
    public float obstacleWeight = 1f;                                       // Peso
    public int obstacleMode = 1;                                            // Qual a funcao de ativacao (Mode = 1, 2, 3)?
    public float obstacleLimiarMinX = 0f, obstacleLimiarMaxX = 1f;          // Quais os Limiares (a < X < b)
    public float obstacleLimiteMinY = 0f, obstacleLimiteMaxY = 1f;          // Quais os Limites (a < Y < b)


    public float logBase = 10f;
    public float gaussMedia = 0.5f;
    public float gaussDesvioPadrao = 0.12f;

    void Update(){

        // Resource
        switch (resourceMode)   // Mode: escolhe a funcao de ativacao
        {
            case 1:
                resourceValue = resourcesDetector.GetLinearOuputRes(resourceLimiarMinX, resourceLimiarMaxX);
                break;
            case 2:
                resourceValue = resourcesDetector.GetGaussianOutputRes(resourceLimiarMinX, resourceLimiarMaxX, gaussMedia, gaussDesvioPadrao);
                break;
            case 3:
                resourceValue = resourcesDetector.GetLogaritmicOutputRes(resourceLimiarMinX, resourceLimiarMaxX, logBase);
                break;
            default:
                if (debugModePrint)
                    Debug.Log("resourceMode != 1, 2, 3");
                break;
        }


        // verificar os limites do output da funcao de ativacao (recursos)
        if (resourceValue < resourceLimiteMinY)
            resourceValue = resourceLimiteMinY;
        else if (resourceValue > resourceLimiteMaxY)
            resourceValue = resourceLimiteMaxY;

        // get sensor data
        resouceAngle = resourcesDetector.GetAngleToClosestResource();


        // Obstacle
        switch (obstacleMode)       // Mode: escolhe a funcao de ativacao
        {
            case 1:
                obstacleValue = blockDetector.GetLinearOutputObs(obstacleLimiarMinX, obstacleLimiarMaxX);
                break;
            case 2:
                obstacleValue = blockDetector.GetGaussianOutputObs(obstacleLimiarMinX, obstacleLimiarMaxX, gaussMedia, gaussDesvioPadrao);
                break;
            case 3:
                obstacleValue = blockDetector.GetLogaritmicOutputObs(obstacleLimiarMinX, obstacleLimiarMaxX, logBase);
                break;
            default:
                if (debugModePrint)
                    Debug.Log("obstacleMode != 1, 2, 3");
                break;
        }

        // verificar os limites do output da funcao de ativacao (obstaculos)
        if (obstacleValue < obstacleLimiteMinY)
            obstacleValue = obstacleLimiteMinY;
        else if (obstacleValue > obstacleLimiteMaxY)
            obstacleValue = obstacleLimiteMaxY;

        // get sensor data
        obstacleAngle = blockDetector.GetAngleToClosestObstacle();

        // apply to the ball
        applyForce(resouceAngle, resourceValue * resourceWeight);                // go towards
        applyForce(obstacleAngle, -1.0f * obstacleValue * obstacleWeight);       // go the opposite direction of a wall
    }
}
