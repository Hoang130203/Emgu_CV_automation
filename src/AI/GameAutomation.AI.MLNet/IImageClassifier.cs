namespace GameAutomation.AI.MLNet;

/// <summary>
/// Interface for ML.NET image classification
/// </summary>
public interface IImageClassifier
{
    /// <summary>
    /// Classifies an image and returns predicted labels with confidence scores
    /// </summary>
    Task<ClassificationResult> ClassifyAsync(byte[] imageData);

    /// <summary>
    /// Trains the model with the provided training data
    /// </summary>
    Task TrainAsync(string trainingDataPath);
}

public class ClassificationResult
{
    public string PredictedLabel { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public Dictionary<string, float> AllScores { get; set; } = new();
}
