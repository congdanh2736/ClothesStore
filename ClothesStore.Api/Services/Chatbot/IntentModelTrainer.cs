// [THÊM MỚI] Chatbot AI - huấn luyện lại mô hình ngay trên server (dùng cho nút "Huấn luyện lại" ở trang admin)
using Microsoft.ML;
using Microsoft.ML.Transforms.Text;

namespace ClothesStore.Api.Services.Chatbot
{
    /* Kết quả 1 lần huấn luyện */
    public class IntentTrainingResult
    {
        public double MicroAccuracy { get; set; }   // tỉ lệ câu đoán đúng trên 20% dữ liệu tách ra
        public double MacroAccuracy { get; set; }   // trung bình độ chính xác của các intent
        public double? HandTestAccuracy { get; set; }
        public int HandTestCount { get; set; }
        public int TrainRows { get; set; }
    }

    /*
     * Giống hệt project ClothesStore.ChatbotTrainner (Program.cs), chỉ khác là chạy trong web:
     *   1. Chia 80% train / 20% kiểm tra -> đo độ chính xác
     *   2. Đo thêm trên câu viết tay (nếu có file test_viet_tay.csv)
     *   3. Train lại trên 100% dữ liệu -> lưu đè MLModels/intent-model.zip (bản cũ giữ ở intent-model.backup.zip)
     * PredictionEnginePool đang theo dõi file .zip (watchForChanges) nên tự dùng mô hình mới ngay.
     * PIPELINE PHẢI GIỐNG BÊN PROJECT TRAINER, nếu sửa bên này thì sửa cả bên kia.
     */
    public static class IntentModelTrainer
    {
        /// <summary>Đọc file CSV 2 cột Text,Label (có dòng tiêu đề)</summary>
        public static List<IntentInput> ReadCsv(string path)
        {
            var ml = new MLContext();
            var data = ml.Data.LoadFromTextFile<IntentInput>(path, separatorChar: ',', hasHeader: true, allowQuoting: true);
            return ml.Data.CreateEnumerable<IntentInput>(data, reuseRowObject: false)
                .Where(x => !string.IsNullOrWhiteSpace(x.Text) && !string.IsNullOrWhiteSpace(x.Label))
                .ToList();
        }

        public static IntentTrainingResult Train(List<IntentInput> rows, List<IntentInput> handTest, string modelPath)
        {
            if (rows.Count < 100) throw new InvalidOperationException("Dữ liệu huấn luyện quá ít (cần ít nhất 100 câu).");

            var ml = new MLContext(seed: 1);
            var data = ml.Data.LoadFromEnumerable(rows);

            // 1. Đánh giá trên 20% dữ liệu tách ra
            var split = ml.Data.TrainTestSplit(data, testFraction: 0.2, seed: 1);
            var model = BuildPipeline(ml).Fit(split.TrainSet);
            var metrics = ml.MulticlassClassification.Evaluate(model.Transform(split.TestSet));
            var result = new IntentTrainingResult
            {
                MicroAccuracy = metrics.MicroAccuracy,
                MacroAccuracy = metrics.MacroAccuracy,
                TrainRows = rows.Count
            };

            // 2. Câu viết tay (bỏ câu có intent không có trong dữ liệu train)
            var labels = rows.Select(x => x.Label).ToHashSet();
            var hand = handTest.Where(x => labels.Contains(x.Label)).ToList();
            if (hand.Count > 0)
            {
                var handMetrics = ml.MulticlassClassification.Evaluate(model.Transform(ml.Data.LoadFromEnumerable(hand)));
                result.HandTestAccuracy = handMetrics.MicroAccuracy;
                result.HandTestCount = hand.Count;
            }

            // 3. Train trên toàn bộ dữ liệu rồi lưu (ghi ra file tạm trước, xong mới thay file thật)
            var finalModel = BuildPipeline(ml).Fit(data);
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
            var tempPath = modelPath + ".tmp";
            ml.Model.Save(finalModel, data.Schema, tempPath);
            if (File.Exists(modelPath))
                File.Copy(modelPath, Path.ChangeExtension(modelPath, null) + ".backup.zip", overwrite: true);
            File.Move(tempPath, modelPath, overwrite: true);
            return result;
        }

        /* Giống BuildPipeline trong ClothesStore.ChatbotTrainner/Program.cs */
        private static IEstimator<ITransformer> BuildPipeline(MLContext ml)
        {
            var textOptions = new TextFeaturizingEstimator.Options
            {
                CaseMode = TextNormalizingEstimator.CaseMode.Lower,
                KeepDiacritics = false,
                KeepPunctuations = false,
                KeepNumbers = true,
                WordFeatureExtractor = new WordBagEstimator.Options { NgramLength = 2, UseAllLengths = true },
                CharFeatureExtractor = new WordBagEstimator.Options { NgramLength = 4, UseAllLengths = true },
                Norm = TextFeaturizingEstimator.NormFunction.L2
            };

            return ml.Transforms.Conversion.MapValueToKey(outputColumnName: "Label", inputColumnName: nameof(IntentInput.Label))
                .Append(ml.Transforms.Text.FeaturizeText("Features", textOptions, nameof(IntentInput.Text)))
                .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(labelColumnName: "Label", featureColumnName: "Features"))
                .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
        }
    }
}
