public float GetFitnessDftfVersao1()
    {
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
            fitness += pesoPoucoImportante * (1.0f/ (this.distanceToBall.Average() < 0.01f ? 0.01f : this.distanceToBall.Average()));

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