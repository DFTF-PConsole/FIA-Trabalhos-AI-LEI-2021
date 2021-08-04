using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public class D31NeuralControler : MonoBehaviour
{
    /* YOUR CODE HERE! */
    public int tempoMaximoSimulacao;


    public RobotUnit agent; // the agent controller we want to use
    public int player;
    public GameObject ball;
    public GameObject MyGoal;
    public GameObject AdversaryGoal;
    public GameObject Adversary;
    public GameObject ScoreSystem;

    
    public int numberOfInputSensores { get; private set; }
    public float[] sensorsInput;


    // Available Information 
    [Header("Environment  Information")]
    public List<float> distanceToBall;
    public List<float> distanceToMyGoal;
    public List<float> distanceToAdversaryGoal;
    public List<float> distanceToAdversary;
    public List<float> distancefromBallToAdversaryGoal;
    public List<float> distancefromBallToMyGoal;
    public List<float> distanceToClosestWall;
    // 
    public List<float> agentSpeed;
    public List<float> ballSpeed;
    public List<float> advSpeed;
    private float FIELD_SIZE =95.0f;
    //
    public float simulationTime = 0;
    public float distanceTravelled = 0.0f;
    public float avgSpeed = 0.0f;
    public float maxSpeed = 0.0f;
    public float currentSpeed = 0.0f;
    public float currentDistance = 0.0f;
    public int hitTheBall;
    public int hitTheWall;
    public int fixedUpdateCalls = 0;
    //
    public float maxSimulTime = 30;
    public bool GameFieldDebugMode = false;
    public bool gameOver = false;
    public bool running = false;

    private Vector3 startPos;
    private Vector3 previousPos;
    private Vector3 ballStartPos;
    private Vector3 ballPreviousPos;
    private Vector3 advPreviousPos;
    private int SampleRate = 1;
    private int countFrames = 0;
    public int GoalsOnAdversaryGoal;
    public int GoalsOnMyGoal;
    public float[] result;



    public NeuralNetwork neuralController;

    private void Awake()
    {
        /* YOUR CODE HERE! */
        tempoMaximoSimulacao = Param.instance.tempoMaximoSimulacao;

        // get the unit controller
        agent = GetComponent<RobotUnit>();
        numberOfInputSensores = 18;
        sensorsInput = new float[numberOfInputSensores];

        startPos = agent.transform.localPosition;
        previousPos = startPos;
        // 2021
        ballPreviousPos = ball.transform.localPosition;
        if (Adversary !=null) { 
            advPreviousPos = Adversary.transform.localPosition;
        }
        
        //Debug.Log(this.neuralController);
        if (GameFieldDebugMode && this.neuralController.weights == null)
        {
            Debug.Log("creating nn..!! ONLY IN GameFieldDebug SCENE THIS SHOULD BE USED!");
            int[] top = { 12, 4, 2 };
            this.neuralController = new NeuralNetwork(top, 0);

        }
        distanceToBall = new List<float>();
        distanceToMyGoal = new List<float>();
        distanceToAdversaryGoal = new List<float>();
        distanceToAdversary = new List<float>();
        distancefromBallToAdversaryGoal = new List<float>();
        distancefromBallToMyGoal = new List<float>();
        distanceToClosestWall = new List<float>();
        agentSpeed = new List<float>();
        ballSpeed = new List<float>();
        advSpeed = new List<float>();
    }


    private void FixedUpdate()
    {
        if(countFrames == 0 && ball != null)
        {
            ballStartPos = ball.transform.localPosition;
            ballPreviousPos = ballStartPos;
        }


        simulationTime += Time.deltaTime;
        if (running && fixedUpdateCalls % 10 == 0)
        {
            // updating sensors
            SensorHandling();
            // move
            result = this.neuralController.process(sensorsInput);
            float angle = result[0] * 180;
            float strength = result[1];            
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;
            dir.z = dir.y;
            dir.y = 0;



            /** DEBUG **
            // debug raycast for the force and angle being applied on the agent
            Vector3 rayDirection = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;
            rayDirection.z = rayDirection.y;
            rayDirection.y = 0;
            
            if (strength > 0)
            {
                Debug.DrawRay(this.transform.position, -rayDirection.normalized * 5, Color.black);
            }
            else
            {
                Debug.DrawRay(this.transform.position, rayDirection.normalized * 5, Color.black);
            }*/

            agent.rb.AddForce(dir * strength * agent.speed); 
            

            // updating game status
            updateGameStatus();

            
            // check method
            if (endSimulationConditions())
            {
                wrapUp();
            }
            countFrames++;
        }
        fixedUpdateCalls++;
    }

    // The ambient variables are created here!
    public void SensorHandling()
    {

        Dictionary<string, ObjectInfo> objects = agent.objectsDetector.GetVisibleObjects();
        sensorsInput[0] = objects["DistanceToBall"].distance / FIELD_SIZE;
        sensorsInput[1] = objects["DistanceToBall"].angle / 360.0f;
        sensorsInput[2] = objects["MyGoal"].distance / FIELD_SIZE;
        sensorsInput[3] = objects["MyGoal"].angle / 360.0f;
        sensorsInput[4] = objects["AdversaryGoal"].distance / FIELD_SIZE;
        sensorsInput[5] = objects["AdversaryGoal"].angle / 360;
        if (objects.ContainsKey("Adversary"))
        {
            sensorsInput[6] = objects["Adversary"].distance / FIELD_SIZE;
            sensorsInput[7] = objects["Adversary"].angle / 360.0f;
        }
        else
        {
            sensorsInput[6] = -1;// -1 == não existe
            sensorsInput[7] = -1;// -1 == não existe
        }
        sensorsInput[8] = Mathf.CeilToInt(Vector3.Distance(ball.transform.localPosition, MyGoal.transform.localPosition)) / FIELD_SIZE; 
        sensorsInput[9] = Mathf.CeilToInt(Vector3.Distance(ball.transform.localPosition, AdversaryGoal.transform.localPosition)) / FIELD_SIZE; 
        sensorsInput[10] = objects["Wall"].distance / FIELD_SIZE;
        sensorsInput[11] = objects["Wall"].angle / 360.0f;

        ////// 
        // Agent speed and angle with previous position
        Vector2 pp = new Vector2(previousPos.x, previousPos.z);
        Vector2 aPos = new Vector2(agent.transform.localPosition.x, agent.transform.localPosition.z);
        aPos = aPos - pp;
        sensorsInput[12] = aPos.magnitude / FIELD_SIZE;
        sensorsInput[13] = Vector2.Angle(aPos, Vector2.right) / 360.0f;
        // Ball speed and angle with previous position
        pp = new Vector2(ballPreviousPos.x, ballPreviousPos.z);
        aPos = new Vector2(ball.transform.localPosition.x, ball.transform.localPosition.z);
        aPos = aPos - pp;
        sensorsInput[14] = aPos.magnitude / FIELD_SIZE;
        sensorsInput[15] = Vector2.Angle(aPos.normalized, Vector2.right) / 360.0f;
        // Adversary Speed and angle with previous position
        if (objects.ContainsKey("Adversary"))
        {
            Vector2 adp = new Vector2(advPreviousPos.x, advPreviousPos.z);
            Vector2 adPos = new Vector2(Adversary.transform.localPosition.x, Adversary.transform.localPosition.z);
            adPos = adPos - adp;
            sensorsInput[16] = adPos.magnitude / FIELD_SIZE;
            sensorsInput[17] = Vector2.Angle(adPos, Vector2.right) / 360.0f;
        }
        else
        {
            sensorsInput[16] = -1;
            sensorsInput[17] = -1;
        }

        if (countFrames % SampleRate == 0)
        {
            distanceToBall.Add(sensorsInput[0]);
            distanceToMyGoal.Add(sensorsInput[2]);
            distanceToAdversaryGoal.Add(sensorsInput[4]);
            distanceToAdversary.Add(sensorsInput[6]);
            distancefromBallToMyGoal.Add(sensorsInput[8]);
            distancefromBallToAdversaryGoal.Add(sensorsInput[9]);
            distanceToClosestWall.Add(sensorsInput[10]);
            // 
            agentSpeed.Add(sensorsInput[12]);
            ballSpeed.Add(sensorsInput[14]);
            advSpeed.Add(sensorsInput[16]);
        }
    }


    public void updateGameStatus()
    {
        // This is the information you can use to build the fitness function. 
        Vector2 pp = new Vector2(previousPos.x, previousPos.z);
        Vector2 aPos = new Vector2(agent.transform.localPosition.x, agent.transform.localPosition.z);
        currentDistance = Vector2.Distance(pp, aPos);
        distanceTravelled += currentDistance;
        hitTheBall = agent.hitTheBall;
        hitTheWall = agent.hitTheWall;

        // update positions!
        previousPos = agent.transform.localPosition;
        ballPreviousPos = ball.transform.localPosition;
        if (Adversary != null)
        {
            advPreviousPos = Adversary.transform.localPosition;
        }


        // get my score
        GoalsOnMyGoal = ScoreSystem.GetComponent<ScoreKeeper>().score[player == 0 ? 1 : 0];
        // get adversary score
        GoalsOnAdversaryGoal = ScoreSystem.GetComponent<ScoreKeeper>().score[player];


    }

    public void wrapUp()
    {
        avgSpeed = avgSpeed / simulationTime;
        gameOver = true;
        running = false;
        countFrames = 0;
        fixedUpdateCalls = 0;
    }

    public static float StdDev(IEnumerable<float> values)
    {
        float ret = 0;
        int count = values.Count();
        if (count > 1)
        {
            //Compute the Average
            float avg = values.Average();

            //Perform the Sum of (value-avg)^2
            float sum = values.Sum(d => (d - avg) * (d - avg));

            //Put it all together
            ret = Mathf.Sqrt(sum / count);
        }
        return ret;
    }

    //******************************************************************************************
    //* FITNESS AND END SIMULATION CONDITIONS *// 
    //******************************************************************************************
    private bool endSimulationConditions()
    {
        // You can modify this to change the length of the simulation of an individual before evaluating it.
        maxSimulTime = tempoMaximoSimulacao;
        
        return simulationTime > this.maxSimulTime;
    }

    public float GetScoreBlue()
    {
        // Fitness function for the Blue player. The code to attribute fitness to individuals should be written here.  
        //* YOUR CODE HERE*//

        float fitness;

        switch (Param.instance.funcaoAptidaoBlue)
        {
            case 1:
                fitness = GetFitnessV1();
                break;
            case 2:
                fitness = GetFitnessV2();
                break;
            case 3:
                fitness = GetFitnessV3();
                break;
            case 4:
                fitness = GetFitnessV4();   // Mais recente
                break;
            case 5:
                fitness = GetFitnessVJCDefesa();   // JC
                break;
            case 6:
                fitness = GetFitnessVJC1V1();   // JC
                break;
            default:
                fitness = GetFitnessV3();  // Melhor Confirmado
                break;
        }

        return fitness;
    }

    public float GetScoreRed()
    {
        // Fitness function for the Red player. The code to attribute fitness to individuals should be written here. 
        //* YOUR CODE HERE*//

        float fitness;

        switch (Param.instance.funcaoAptidaoBlue)
        {
            case 1:
                fitness = GetFitnessV1();
                break;
            case 2:
                fitness = GetFitnessV2();
                break;
            case 3:
                fitness = GetFitnessV3();
                break;
            case 4:
                fitness = GetFitnessV4();   // Mais recente
                break;
            case 5:
                fitness = GetFitnessVJCDefesa();   // JC
                break;
            case 6:
                fitness = GetFitnessVJC1V1();   // JC
                break;
            default:
                fitness = GetFitnessV3();  // Melhor Confirmado
                break;
        }

        if (Param.instance.isLogActivo)
        {
            Debug.Log("--------- RED BRUTO -----------");

            Debug.Log("distanceToBall-Average: " + this.distanceToBall.Average());
            Debug.Log("distanceToBall-Max: " + this.distanceToBall.Max());
            Debug.Log("distanceToBall-Min: " + this.distanceToBall.Min());

            Debug.Log("distanceToMyGoal-Average: " + this.distanceToMyGoal.Average());
            Debug.Log("distanceToMyGoal-Min: " + this.distanceToMyGoal.Min());

            Debug.Log("distanceToAdversaryGoal-Average: " + this.distanceToAdversaryGoal.Average());
            Debug.Log("distanceToAdversaryGoal-Min: " + this.distanceToAdversaryGoal.Min());

            Debug.Log("distanceToAdversary-Average: " + this.distanceToAdversary.Average());
            Debug.Log("distanceToAdversary-Min: " + this.distanceToAdversary.Min());

            Debug.Log("distancefromBallToMyGoal-Average: " + this.distancefromBallToMyGoal.Average());
            Debug.Log("distancefromBallToMyGoal-Min: " + this.distancefromBallToMyGoal.Min());

            Debug.Log("distancefromBallToAdversaryGoal-Average: " + this.distancefromBallToAdversaryGoal.Average());
            Debug.Log("distancefromBallToAdversaryGoal-Max: " + this.distancefromBallToAdversaryGoal.Max());
            Debug.Log("distancefromBallToAdversaryGoal-Min: " + this.distancefromBallToAdversaryGoal.Min());

            Debug.Log("distanceToClosestWall-Average: " + this.distanceToClosestWall.Average());
            Debug.Log("distanceToClosestWall-Min: " + this.distanceToClosestWall.Min());

            Debug.Log("agentSpeed-Average: " + this.agentSpeed.Average());
            Debug.Log("agentSpeed-Max: " + this.agentSpeed.Max());
            Debug.Log("agentSpeed-Min: " + this.agentSpeed.Min());

            Debug.Log("ballSpeed-Average: " + this.ballSpeed.Average());
            Debug.Log("ballSpeed-Min: " + this.ballSpeed.Min());

            Debug.Log("advSpeed-Average: " + this.advSpeed.Average());
            Debug.Log("advSpeed-Min: " + this.advSpeed.Min());

            Debug.Log("distanceTravelled: " + this.distanceTravelled);

            Debug.Log("hitTheBall: " + this.hitTheBall);

            Debug.Log("hitTheWall: " + this.hitTheWall);

            Debug.Log("GoalsOnMyGoal: " + this.GoalsOnMyGoal);

            Debug.Log("GoalsOnAdversaryGoal: " + this.GoalsOnAdversaryGoal);

            Debug.Log("_______________________");

            Debug.Log("fitness: " + fitness);

            Debug.Log("--------- FIM BRUTO ----------");
        }

        return fitness;
    }


    public float GetFitnessV1()
    {
        // ABANDONADO   (contem erros)

        float fitness = 0.0f;
        float pesoNadaImportante = 1.0f;
        float pesoPoucoImportante = 2.0f;
        float pesoImportante = 10.0f;
        float pesoMuitoImportante = 100.0f;
        float pesoMegaImportante = 1000.0f;
        float pesoNegativo = -1.0f;

        fitness += pesoNegativo * pesoMegaImportante * this.GoalsOnMyGoal;

        fitness += pesoMegaImportante * this.GoalsOnAdversaryGoal;

        fitness += pesoImportante * this.hitTheBall;

        fitness += pesoPoucoImportante * this.distanceTravelled;

        if (this.distanceToBall.Average() > 3.0f)
            fitness += pesoNegativo * pesoPoucoImportante * this.distanceToBall.Average();
        else
            fitness += pesoPoucoImportante * (1.0f / (this.distanceToBall.Average() < 0.01f ? 0.01f : this.distanceToBall.Average()));

        if (this.distancefromBallToAdversaryGoal.Average() > 2.0f)
            fitness += pesoNegativo * pesoImportante * this.distancefromBallToAdversaryGoal.Average();
        else
            fitness += pesoImportante * (1.0f / (this.distancefromBallToAdversaryGoal.Average() < 0.001f ? 0.001f : this.distancefromBallToAdversaryGoal.Average()));

        if (this.agentSpeed.Average() > pesoMuitoImportante)
            fitness += pesoMuitoImportante;
        else
            fitness += pesoNadaImportante * agentSpeed.Average();

        if (this.ballSpeed.Average() > pesoImportante)
            fitness += pesoImportante;
        else
            fitness += pesoNadaImportante * ballSpeed.Average();

        if (this.distanceToMyGoal.Average() < 0.25f)
            fitness += pesoNegativo * pesoPoucoImportante * (1.0f / (this.distanceToMyGoal.Average() < 0.001f ? 0.001f : this.distanceToMyGoal.Average()));

        if (this.distancefromBallToMyGoal.Average() > 1.0f)
            fitness += pesoPoucoImportante * this.distancefromBallToMyGoal.Average();
        else
            fitness += pesoNegativo * pesoPoucoImportante * (1.0f / (this.distancefromBallToMyGoal.Average() < 0.001f ? 0.001f : this.distancefromBallToMyGoal.Average()));

        return fitness;
    }


    public float GetFitnessV2()
    {
        // ABANDONADO (contem erros)

        float fitness = 0.0f;
        float pesoNadaImportante = 1.0f;
        float pesoPoucoImportante = 2.0f;
        float pesoImportante = 10.0f;
        float pesoMuitoImportante = 100.0f;
        float pesoMegaImportante = 2000.0f;
        float pesoNegativo = -1.0f;

        fitness += pesoNegativo * pesoMegaImportante * this.GoalsOnMyGoal;

        fitness += pesoMegaImportante * this.GoalsOnAdversaryGoal;

        fitness += pesoImportante * this.hitTheBall;

        fitness += pesoPoucoImportante * this.distanceTravelled;

        if (this.distanceToBall.Average() > 5.0f)
            fitness += pesoNegativo * pesoPoucoImportante * this.distanceToBall.Average();
        else
            fitness += pesoPoucoImportante * (5.0f / (this.distanceToBall.Average() < 0.1f ? 0.1f : this.distanceToBall.Average()));

        if (this.distancefromBallToAdversaryGoal.Average() > 5.0f)
            fitness += pesoNegativo * pesoImportante * this.distancefromBallToAdversaryGoal.Average();
        else
            fitness += pesoImportante * (4.0f / (this.distancefromBallToAdversaryGoal.Average() < 0.01f ? 0.01f : this.distancefromBallToAdversaryGoal.Average()));

        if (this.agentSpeed.Average() > pesoMuitoImportante)
            fitness += pesoMuitoImportante;
        else
            fitness += pesoNadaImportante * agentSpeed.Average();

        if (this.ballSpeed.Max() > pesoImportante)
            fitness += pesoImportante;
        else
            fitness += pesoImportante * ballSpeed.Max();    // Temp

        if (this.distanceToMyGoal.Average() < 0.25f)
            fitness += pesoNegativo * pesoPoucoImportante * (1.0f / (this.distanceToMyGoal.Average() < 0.01f ? 0.01f : this.distanceToMyGoal.Average()));

        if (this.distancefromBallToMyGoal.Average() > 1.0f)
            fitness += pesoImportante * this.distancefromBallToMyGoal.Average();
        else
            fitness += pesoNegativo * pesoPoucoImportante * (1.0f / (this.distancefromBallToMyGoal.Average() < 0.01f ? 0.01f : this.distancefromBallToMyGoal.Average()));

        if (this.distanceToAdversaryGoal.Average() < 1.0f)
            fitness += pesoNegativo * pesoPoucoImportante * (2.0f / (this.distanceToAdversaryGoal.Average() < 0.01f ? 0.01f : this.distanceToAdversaryGoal.Average()));

        fitness += pesoNegativo * pesoNadaImportante * this.hitTheWall;

        return fitness;
    }


    public float GetFitnessV3()
    {
        // Reformalação
        

        // Global

        float fitness = 0.0f;
        float pesoNegativo = -1.0f;
        float valorMin = 0.5f;


        // Normalizar

        float distanceToBallNormalizado = this.distanceToBall.Min() * Param.instance.normalizarValores;
        float distanceToMyGoalNormalizado = this.distanceToMyGoal.Min() * Param.instance.normalizarValores;
        float distanceToAdversaryGoalNormalizado = this.distanceToAdversaryGoal.Min() * Param.instance.normalizarValores;
        float distanceToAdversaryNormalizado = this.distanceToAdversary.Min() * Param.instance.normalizarValores;
        float distancefromBallToMyGoalNormalizado = this.distancefromBallToMyGoal.Min() * Param.instance.normalizarValores;
        float distancefromBallToAdversaryGoalNormalizado = this.distancefromBallToAdversaryGoal.Min() * Param.instance.normalizarValores;
        float distanceToClosestWallNormalizado = this.distanceToClosestWall.Average() * Param.instance.normalizarValores;
        float agentSpeedNormalizado = this.agentSpeed.Max() * Param.instance.normalizarValores;
        float ballSpeedNormalizado = this.ballSpeed.Max() * Param.instance.normalizarValores;
        float advSpeedNormalizado = this.advSpeed.Average() * Param.instance.normalizarValores;
        float distanceTravelledNormalizado = this.distanceTravelled > Param.instance.normalizarValores ? Param.instance.normalizarValores : this.distanceTravelled;
        float hitTheBallNormalizado = this.hitTheBall * (Param.instance.normalizarValores / 1000.0f);   // pode sair do range
        float hitTheWallNormalizado = this.hitTheWall * (Param.instance.normalizarValores / 1000.0f);   // pode sair do range
        float GoalsOnMyGoalNormalizado = this.GoalsOnMyGoal * Param.instance.normalizarValores;
        float GoalsOnAdversaryGoalNormalizado = this.GoalsOnAdversaryGoal * Param.instance.normalizarValores;


        if (Param.instance.isLogActivo)
        {
            Debug.Log("--------- RELATIVO -----------");
            Debug.Log("distanceToBallNormalizado: " + distanceToBallNormalizado);
            Debug.Log("distanceToMyGoalNormalizado: " + distanceToMyGoalNormalizado);
            Debug.Log("distanceToAdversaryGoalNormalizado: " + distanceToAdversaryGoalNormalizado);
            Debug.Log("distanceToAdversaryNormalizado: " + distanceToAdversaryNormalizado);
            Debug.Log("distancefromBallToMyGoalNormalizado: " + distancefromBallToMyGoalNormalizado);
            Debug.Log("distancefromBallToAdversaryGoalNormalizado: " + distancefromBallToAdversaryGoalNormalizado);
            Debug.Log("distanceToClosestWallNormalizado: " + distanceToClosestWallNormalizado);
            Debug.Log("agentSpeedNormalizado: " + agentSpeedNormalizado);
            Debug.Log("ballSpeedNormalizado: " + ballSpeedNormalizado);
            Debug.Log("advSpeedNormalizado: " + advSpeedNormalizado);
            Debug.Log("distanceTravelledNormalizado: " + distanceTravelledNormalizado);
            Debug.Log("hitTheBallNormalizado: " + hitTheBallNormalizado);
            Debug.Log("hitTheWallNormalizado: " + hitTheWallNormalizado);
            Debug.Log("GoalsOnMyGoalNormalizado: " + GoalsOnMyGoalNormalizado);
            Debug.Log("GoalsOnAdversaryGoalNormalizado: " + GoalsOnAdversaryGoalNormalizado);
            Debug.Log("---------- FIM RELATIVO -----------");
        }


        // Calcular fitness (cuidado com as divisao por 0)

        fitness += (Param.instance.normalizarValores / (distanceToBallNormalizado < valorMin ? valorMin : distanceToBallNormalizado)) * GetPesoByN(Param.instance.distanceToBallPeso);
        
        fitness += (distanceToMyGoalNormalizado < 1.0f ? 0.0f : 1.0f) * GetPesoByN(Param.instance.distanceToMyGoalPeso);
        
        fitness += (Param.instance.normalizarValores / (distanceToAdversaryGoalNormalizado < Param.instance.normalizarValores * (0.333f) ? Param.instance.normalizarValores * (0.333f) : distanceToAdversaryGoalNormalizado)) * GetPesoByN(Param.instance.distanceToAdversaryGoalPeso);
        
        if (Param.instance.isAdvAtivo)
            fitness += (Param.instance.normalizarValores / (distanceToAdversaryNormalizado < valorMin ? valorMin : distanceToAdversaryNormalizado)) * GetPesoByN(Param.instance.distanceToAdversaryPeso); // X

        fitness += (distancefromBallToMyGoalNormalizado) * GetPesoByN(Param.instance.distancefromBallToMyGoalPeso);

        fitness += (Param.instance.normalizarValores / (distancefromBallToAdversaryGoalNormalizado < valorMin ? valorMin : distancefromBallToAdversaryGoalNormalizado)) * GetPesoByN(Param.instance.distancefromBallToAdversaryGoalPeso);

        fitness += (distanceToClosestWallNormalizado) * GetPesoByN(Param.instance.distanceToClosestWallPeso);   // X

        fitness += (agentSpeedNormalizado) * GetPesoByN(Param.instance.agentSpeedPeso);

        fitness += (ballSpeedNormalizado) * GetPesoByN(Param.instance.ballSpeedPeso);

        if (Param.instance.isAdvAtivo)
            fitness += (advSpeedNormalizado) * GetPesoByN(Param.instance.advSpeedPeso); // X

        fitness += (distanceTravelledNormalizado) * GetPesoByN(Param.instance.distanceTravelledPeso);

        fitness += (hitTheBallNormalizado) * GetPesoByN(Param.instance.hitTheBallPeso);

        fitness += (hitTheWallNormalizado == 0.0f ? 1.0f : 0.0f) * GetPesoByN(Param.instance.hitTheWallPeso);

        fitness += (GoalsOnMyGoalNormalizado) * GetPesoByN(Param.instance.GoalsOnMyGoalPeso) * pesoNegativo;

        fitness += (GoalsOnAdversaryGoalNormalizado) * GetPesoByN(Param.instance.GoalsOnAdversaryGoalPeso);

        return fitness;
    }



    public float GetFitnessV4()
    {
        // Melhoramentos face ao v3


        // Global

        float fitness = 0.0f;
        float pesoNegativo = -1.0f;
        float valorMin = 0.5f;

        // ESPECIAIS (Alt v3 -> v4)
        float valorMegaMin = 1.0f / Param.instance.normalizarValores;


        // Normalizar

        float distanceToBallNormalizado = this.distanceToBall.Min() * Param.instance.normalizarValores;
        float distanceToMyGoalNormalizado = this.distanceToMyGoal.Min() * Param.instance.normalizarValores;
        float distanceToAdversaryGoalNormalizado = this.distanceToAdversaryGoal.Min() * Param.instance.normalizarValores;
        float distanceToAdversaryNormalizado = this.distanceToAdversary.Average() * Param.instance.normalizarValores;
        float distancefromBallToMyGoalNormalizado = this.distancefromBallToMyGoal.Min() * Param.instance.normalizarValores;
        float distancefromBallToAdversaryGoalNormalizado = this.distancefromBallToAdversaryGoal.Min() * Param.instance.normalizarValores;
        float distanceToClosestWallNormalizado = this.distanceToClosestWall.Average() * Param.instance.normalizarValores;
        float agentSpeedNormalizado = this.agentSpeed.Max() * Param.instance.normalizarValores;
        float ballSpeedNormalizado = this.ballSpeed.Max() * Param.instance.normalizarValores;
        float advSpeedNormalizado = this.advSpeed.Average() * Param.instance.normalizarValores;
        float distanceTravelledNormalizado = this.distanceTravelled > Param.instance.normalizarValores ? Param.instance.normalizarValores : this.distanceTravelled;
        float hitTheBallNormalizado = this.hitTheBall * (Param.instance.normalizarValores / 1000.0f);   // pode sair do range
        float hitTheWallNormalizado = this.hitTheWall * (Param.instance.normalizarValores / 1000.0f);   // pode sair do range
        float GoalsOnMyGoalNormalizado = this.GoalsOnMyGoal * Param.instance.normalizarValores;
        float GoalsOnAdversaryGoalNormalizado = this.GoalsOnAdversaryGoal * Param.instance.normalizarValores;

        // ESPECIAIS (Alt v3 -> v4)
        float distanceToBallNormalizadoAverage = this.distanceToBall.Average() * Param.instance.normalizarValores;
        float distancefromBallToMyGoalNormalizadoAverage = this.distancefromBallToMyGoal.Average() * Param.instance.normalizarValores;
        float agentSpeedNormalizadoAverage = this.agentSpeed.Average() * Param.instance.normalizarValores;
        float distancefromBallToAdversaryGoalNormalizadoAverage = this.distancefromBallToAdversaryGoal.Average() * Param.instance.normalizarValores;
        float distanceToAdversaryGoalNormalizadoAverage = this.distanceToAdversaryGoal.Average() * Param.instance.normalizarValores;



        if (Param.instance.isLogActivo)
        {
            Debug.Log("--------- RELATIVO -----------");
            Debug.Log("distanceToBallNormalizado: " + distanceToBallNormalizado);
            Debug.Log("distanceToMyGoalNormalizado: " + distanceToMyGoalNormalizado);
            Debug.Log("distanceToAdversaryGoalNormalizado: " + distanceToAdversaryGoalNormalizado);
            Debug.Log("distanceToAdversaryNormalizado: " + distanceToAdversaryNormalizado);
            Debug.Log("distancefromBallToMyGoalNormalizado: " + distancefromBallToMyGoalNormalizado);
            Debug.Log("distancefromBallToAdversaryGoalNormalizado: " + distancefromBallToAdversaryGoalNormalizado);
            Debug.Log("distanceToClosestWallNormalizado: " + distanceToClosestWallNormalizado);
            Debug.Log("agentSpeedNormalizado: " + agentSpeedNormalizado);
            Debug.Log("ballSpeedNormalizado: " + ballSpeedNormalizado);
            Debug.Log("advSpeedNormalizado: " + advSpeedNormalizado);
            Debug.Log("distanceTravelledNormalizado: " + distanceTravelledNormalizado);
            Debug.Log("hitTheBallNormalizado: " + hitTheBallNormalizado);
            Debug.Log("hitTheWallNormalizado: " + hitTheWallNormalizado);
            Debug.Log("GoalsOnMyGoalNormalizado: " + GoalsOnMyGoalNormalizado);
            Debug.Log("GoalsOnAdversaryGoalNormalizado: " + GoalsOnAdversaryGoalNormalizado);

            // ESPECIAIS (Alt v3 -> v4)
            Debug.Log("distanceToBallNormalizadoAverage: " + distanceToBallNormalizadoAverage);
            Debug.Log("distancefromBallToMyGoalNormalizadoAverage: " + distancefromBallToMyGoalNormalizadoAverage);
            Debug.Log("agentSpeedNormalizadoAverage: " + agentSpeedNormalizadoAverage);
            Debug.Log("distancefromBallToAdversaryGoalNormalizadoAverage: " + distancefromBallToAdversaryGoalNormalizadoAverage);

            Debug.Log("---------- FIM RELATIVO -----------");
        }


        // Calcular fitness (cuidado com as divisao por 0)

        fitness += (Param.instance.normalizarValores / (distanceToBallNormalizado < valorMin ? valorMin : distanceToBallNormalizado)) * GetPesoByN(Param.instance.distanceToBallPeso);
        // (Alt v3 -> v4)
        if (distanceToBallNormalizado < valorMin)
            fitness += (1.0f / (distanceToBallNormalizado < valorMegaMin ? valorMegaMin : distanceToBallNormalizado));

        distanceToMyGoalNormalizado = distanceToMyGoalNormalizado > Param.instance.normalizarValores * (0.5f) ? Param.instance.normalizarValores * (0.5f) : distanceToMyGoalNormalizado;
        fitness += (distanceToMyGoalNormalizado < 1.0f ? 0.0f : distanceToMyGoalNormalizado) * GetPesoByN(Param.instance.distanceToMyGoalPeso);

        fitness += (Param.instance.normalizarValores / (distanceToAdversaryGoalNormalizado < Param.instance.normalizarValores * (0.4f) ? Param.instance.normalizarValores * (0.4f) : distanceToAdversaryGoalNormalizado)) * GetPesoByN(Param.instance.distanceToAdversaryGoalPeso);

        if (Param.instance.isAdvAtivo)
            fitness += (Param.instance.normalizarValores / (distanceToAdversaryNormalizado < valorMin ? valorMin : distanceToAdversaryNormalizado)) * GetPesoByN(Param.instance.distanceToAdversaryPeso); // X

        fitness += (distancefromBallToMyGoalNormalizado) * GetPesoByN(Param.instance.distancefromBallToMyGoalPeso);

        fitness += (Param.instance.normalizarValores / (distancefromBallToAdversaryGoalNormalizado < valorMin ? valorMin : distancefromBallToAdversaryGoalNormalizado)) * GetPesoByN(Param.instance.distancefromBallToAdversaryGoalPeso);

        fitness += (distanceToClosestWallNormalizado) * GetPesoByN(Param.instance.distanceToClosestWallPeso);   // X

        fitness += (agentSpeedNormalizado) * GetPesoByN(Param.instance.agentSpeedPeso);

        fitness += (ballSpeedNormalizado) * GetPesoByN(Param.instance.ballSpeedPeso);

        if (Param.instance.isAdvAtivo)
            fitness += (advSpeedNormalizado) * GetPesoByN(Param.instance.advSpeedPeso); // X

        fitness += (distanceTravelledNormalizado) * GetPesoByN(Param.instance.distanceTravelledPeso);

        fitness += (hitTheBallNormalizado) * GetPesoByN(Param.instance.hitTheBallPeso);

        // (Alt v3 -> v4)
        fitness += (Param.instance.normalizarValores / (hitTheWallNormalizado < 1.0f ? 1.0f : hitTheWallNormalizado)) * GetPesoByN(Param.instance.hitTheWallPeso);

        fitness += (GoalsOnMyGoalNormalizado) * GetPesoByN(Param.instance.GoalsOnMyGoalPeso) * pesoNegativo;

        fitness += (GoalsOnAdversaryGoalNormalizado) * GetPesoByN(Param.instance.GoalsOnAdversaryGoalPeso);


        // ESPECIAIS (Alt v3 -> v4)
        fitness += (Param.instance.normalizarValores / (distanceToBallNormalizadoAverage < valorMin ? valorMin : distanceToBallNormalizadoAverage)) * GetPesoByN(Param.instance.distanceToBallPeso) / 2.0f;
        fitness += (distancefromBallToMyGoalNormalizadoAverage) * GetPesoByN(Param.instance.distancefromBallToMyGoalPeso) / 2.0f;
        fitness += (agentSpeedNormalizadoAverage) * GetPesoByN(Param.instance.agentSpeedPeso) / 2.0f;
        fitness += (Param.instance.normalizarValores / (distancefromBallToAdversaryGoalNormalizadoAverage < valorMin ? valorMin : distancefromBallToAdversaryGoalNormalizadoAverage)) * GetPesoByN(Param.instance.distancefromBallToAdversaryGoalPeso) / 2.0f;


        // ESPECIAIS (Alt v3 -> v4) manter a bola entre baliza e jogador
        fitness += (((distanceToAdversaryGoalNormalizadoAverage * 0.95f) / ((distancefromBallToAdversaryGoalNormalizadoAverage < valorMin ? valorMin : distancefromBallToAdversaryGoalNormalizadoAverage) )) * GetPesoByN(3));
        fitness += ((distancefromBallToAdversaryGoalNormalizadoAverage / ((distanceToAdversaryGoalNormalizadoAverage < valorMin ? valorMin : distanceToAdversaryGoalNormalizadoAverage) * 0.95f) ) * GetPesoByN(3) * pesoNegativo);
        fitness += (Param.instance.normalizarValores / (distancefromBallToMyGoalNormalizado < valorMin ? valorMin : distancefromBallToMyGoalNormalizado)) * GetPesoByN(Param.instance.distancefromBallToMyGoalPeso) * pesoNegativo;


        return fitness;
    }



    public float GetPesoByN(int pesoN)
    {
        switch (pesoN)
        {
            case 1:
                return Param.instance.peso1NadaImportante;
            case 2:
                return Param.instance.peso2PoucoImportante;
            case 3:
                return Param.instance.peso3Importante;
            case 4:
                return Param.instance.peso4MuitoImportante;
            case 5:
                return Param.instance.peso5MegaImportante;
            default:
                return 0;
        }
    }



    public float GetFitnessVJCDefesa()
    {
        float ballSpeedNormalizado = this.ballSpeed.Max() * Param.instance.normalizarValores;
        float distanceToBallNormalizado = this.distanceToBall.Min() * Param.instance.normalizarValores;
        float distancefromBallToAdversaryGoalNormalizado = this.distancefromBallToAdversaryGoal.Min() * Param.instance.normalizarValores;
        float distancefromBallToMyGoalNormalizado = this.distancefromBallToMyGoal.Min() * Param.instance.normalizarValores;
        float hitTheBallNormalizado = this.hitTheBall * (Param.instance.normalizarValores / 1000.0f);   // pode sair do range
        float hitTheWallNormalizado = this.hitTheWall * (Param.instance.normalizarValores / 1000.0f);   // pode sair do range
        float agentSpeedNormalizado = this.agentSpeed.Max() * Param.instance.normalizarValores;

        float fitness = 0.0f;
        //fitness += 10* distanceToBall.Min() -GoalsOnMyGoal * 20 + GoalsOnAdversaryGoal * 20 - hitTheWall * 10 + agentSpeed.Average()*5 + distancefromBallToAdversaryGoal.Min() *10 - distancefromBallToMyGoal.Min()*10;
        fitness += 1000 * -GoalsOnMyGoal + 10 * distancefromBallToMyGoalNormalizado + hitTheBallNormalizado * 40 + agentSpeedNormalizado * 10 - hitTheWallNormalizado * 40;
        fitness += 1 / ballSpeedNormalizado * 10;
        fitness += (Param.instance.normalizarValores / (distanceToBallNormalizado < 0.5f ? 0.5f : distanceToBallNormalizado)) * 20.0f;
        fitness += 1000 * GoalsOnAdversaryGoal;
        fitness += (100f - distancefromBallToAdversaryGoalNormalizado) * 10;
        return fitness;
    }



    public float GetFitnessVJC1V1()
    {
        // UTILIZADO MAPA 1


        // Global

        float fitness = 0.0f;
        float pesoNegativo = -1.0f;
        float valorMin = 0.5f;


        // Normalizar

        float distanceToBallNormalizado = this.distanceToBall.Min() * Param.instance.normalizarValores;
        float distanceToMyGoalNormalizado = this.distanceToMyGoal.Min() * Param.instance.normalizarValores;
        float distanceToMyGoalAvgNormalizado = this.distanceToMyGoal.Average() * Param.instance.normalizarValores;
        float distanceToMyGoalMaxNormalizado = this.distanceToMyGoal.Max() * Param.instance.normalizarValores;
        float distanceToAdversaryGoalNormalizado = this.distanceToAdversaryGoal.Min() * Param.instance.normalizarValores;
        float distanceToAdversaryNormalizado = this.distanceToAdversary.Min() * Param.instance.normalizarValores;
        float distancefromBallToMyGoalNormalizado = this.distancefromBallToMyGoal.Min() * Param.instance.normalizarValores;
        float distancefromBallToAdversaryGoalNormalizado = this.distancefromBallToAdversaryGoal.Min() * Param.instance.normalizarValores;
        float distanceToClosestWallNormalizado = this.distanceToClosestWall.Average() * Param.instance.normalizarValores;
        float agentSpeedNormalizado = this.agentSpeed.Max() * Param.instance.normalizarValores;
        float ballSpeedNormalizado = this.ballSpeed.Max() * Param.instance.normalizarValores;
        float advSpeedNormalizado = this.advSpeed.Average() * Param.instance.normalizarValores;
        float distanceTravelledNormalizado = this.distanceTravelled > Param.instance.normalizarValores ? Param.instance.normalizarValores : this.distanceTravelled;
        float hitTheBallNormalizado = this.hitTheBall * (Param.instance.normalizarValores / 1000.0f);   // pode sair do range
        float hitTheWallNormalizado = this.hitTheWall * (Param.instance.normalizarValores / 1000.0f);   // pode sair do range
        float GoalsOnMyGoalNormalizado = this.GoalsOnMyGoal * Param.instance.normalizarValores;
        float GoalsOnAdversaryGoalNormalizado = this.GoalsOnAdversaryGoal * Param.instance.normalizarValores;


        if (Param.instance.isLogActivo)
        {
            Debug.Log("--------- RELATIVO -----------");
            Debug.Log("distanceToBallNormalizado: " + distanceToBallNormalizado);
            Debug.Log("distanceToMyGoalNormalizado: " + distanceToMyGoalNormalizado);
            Debug.Log("distanceToAdversaryGoalNormalizado: " + distanceToAdversaryGoalNormalizado);
            Debug.Log("distanceToAdversaryNormalizado: " + distanceToAdversaryNormalizado);
            Debug.Log("distancefromBallToMyGoalNormalizado: " + distancefromBallToMyGoalNormalizado);
            Debug.Log("distancefromBallToAdversaryGoalNormalizado: " + distancefromBallToAdversaryGoalNormalizado);
            Debug.Log("distanceToClosestWallNormalizado: " + distanceToClosestWallNormalizado);
            Debug.Log("agentSpeedNormalizado: " + agentSpeedNormalizado);
            Debug.Log("ballSpeedNormalizado: " + ballSpeedNormalizado);
            Debug.Log("advSpeedNormalizado: " + advSpeedNormalizado);
            Debug.Log("distanceTravelledNormalizado: " + distanceTravelledNormalizado);
            Debug.Log("hitTheBallNormalizado: " + hitTheBallNormalizado);
            Debug.Log("hitTheWallNormalizado: " + hitTheWallNormalizado);
            Debug.Log("GoalsOnMyGoalNormalizado: " + GoalsOnMyGoalNormalizado);
            Debug.Log("GoalsOnAdversaryGoalNormalizado: " + GoalsOnAdversaryGoalNormalizado);
            Debug.Log("---------- FIM RELATIVO -----------");
        }


        // Calcular fitness (cuidado com as divisao por 0)

        fitness += (Param.instance.normalizarValores / (distanceToBallNormalizado < valorMin ? valorMin : distanceToBallNormalizado)) * GetPesoByN(Param.instance.distanceToBallPeso);

        fitness += (distanceToMyGoalNormalizado < 1.0f ? 0.0f : 1.0f) * GetPesoByN(Param.instance.distanceToMyGoalPeso);

        fitness += (Param.instance.normalizarValores / (distanceToAdversaryGoalNormalizado < Param.instance.normalizarValores * (0.333f) ? Param.instance.normalizarValores * (0.333f) : distanceToAdversaryGoalNormalizado)) * GetPesoByN(Param.instance.distanceToAdversaryGoalPeso);

        if (Param.instance.isAdvAtivo)
            fitness += (Param.instance.normalizarValores / (distanceToAdversaryNormalizado < valorMin ? valorMin : distanceToAdversaryNormalizado)) * GetPesoByN(Param.instance.distanceToAdversaryPeso); // X

        fitness += (distancefromBallToMyGoalNormalizado) * GetPesoByN(Param.instance.distancefromBallToMyGoalPeso);

        fitness += (Param.instance.normalizarValores / (distancefromBallToAdversaryGoalNormalizado < valorMin ? valorMin : distancefromBallToAdversaryGoalNormalizado)) * GetPesoByN(Param.instance.distancefromBallToAdversaryGoalPeso);

        fitness += (distanceToClosestWallNormalizado) * GetPesoByN(Param.instance.distanceToClosestWallPeso);   // X

        fitness += (agentSpeedNormalizado) * GetPesoByN(Param.instance.agentSpeedPeso);

        fitness += (ballSpeedNormalizado) * GetPesoByN(Param.instance.ballSpeedPeso);

        if (Param.instance.isAdvAtivo)
            fitness += (advSpeedNormalizado) * GetPesoByN(Param.instance.advSpeedPeso); // X

        fitness += (distanceTravelledNormalizado) * GetPesoByN(Param.instance.distanceTravelledPeso);

        fitness += (hitTheBallNormalizado) * GetPesoByN(Param.instance.hitTheBallPeso);

        fitness += (hitTheWallNormalizado == 0.0f ? 1.0f : 0.0f) * GetPesoByN(Param.instance.hitTheWallPeso);

        fitness += (GoalsOnMyGoalNormalizado) * GetPesoByN(Param.instance.GoalsOnMyGoalPeso) * pesoNegativo;

        fitness += (GoalsOnAdversaryGoalNormalizado) * GetPesoByN(Param.instance.GoalsOnAdversaryGoalPeso);

        if (distanceToMyGoalMaxNormalizado > 0.2f && (distancefromBallToMyGoalNormalizado > distanceToMyGoalMaxNormalizado))
            fitness += (distancefromBallToMyGoalNormalizado - distanceToMyGoalMaxNormalizado) * GetPesoByN(Param.instance.distanceToBallPeso);
        else if (distanceToMyGoalNormalizado < 0.2f)
            fitness -= distanceToMyGoalMaxNormalizado * GetPesoByN(Param.instance.distanceToMyGoalPeso);
        return fitness;
    }

}