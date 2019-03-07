namespace FunctionApp1.Entitity
{
    public class FaceRectangle
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
    }
    public class Emotion
    {
        public float Anger { get; set; }
        public float Contempt { get; set; }
        public float Disgust { get; set; }
        public float Fear { get; set; }
        public float Happiness { get; set; }
        public float Neutral { get; set; }
        public float Sadness { get; set; }
        public float Surprise { get; set; }
    }
    public class FaceAttributes
    {
        public Emotion Emotion { get; set; }
    }
    public class Face
    {
        public FaceRectangle FaceRectangle { get; set; }
        public FaceAttributes FaceAttributes { get; set; }
    }
 
}
