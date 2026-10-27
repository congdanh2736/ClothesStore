/*
 * ============================================================================
 *  Program.cs - HUẤN LUYỆN MÔ HÌNH NHẬN DIỆN Ý ĐỊNH (INTENT) CHO CHATBOT
 * ============================================================================
 *  Các bước:
 *    1. Đọc dữ liệu Data/intents.csv (2 cột: Text = câu hỏi, Label = intent)
 *    2. Chia 80% để train, 20% để kiểm tra
 *    3. Train mô hình phân loại văn bản bằng ML.NET
 *    4. Đánh giá: độ chính xác + bảng nhầm lẫn (intent nào hay bị đoán nhầm)
 *    5. Kiểm tra thêm bằng câu viết tay (Data/test_viet_tay.csv) - con số đưa vào báo cáo
 *    6. Train lại trên TOÀN BỘ dữ liệu rồi lưu ra ClothesStore.Api/MLModels/intent-model.zip
 *    7. Cho gõ thử câu hỏi ngay trong cửa sổ console
 * ============================================================================
 */
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using System.Data;
using System.Diagnostics;

// ---------------------------------------------------------------- Đường dẫn
// Chương trình chạy trong bin/Debug/net10.0 -> lùi 3 cấp là thư mục project Trainer
var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var trainFile = Path.Combine(projectDir, "Data", "intents.csv");
var handTestFile = Path.Combine(projectDir, "Data", "test_viet_tay.csv");
// Thư mục ClothesStore.Api nằm cạnh thư mục Trainer (cùng cấp với file ClothesStore.slnx)
var modelFile = Path.GetFullPath(Path.Combine(projectDir, "..", "ClothesStore.Api", "MLModels", "intent-model.zip"));

if (!File.Exists(trainFile))
{
    Console.WriteLine($"Không tìm thấy file dữ liệu: {trainFile}");
    return;
}

var ml = new MLContext(seed: 1); // seed cố định -> chạy lại ra cùng kết quả

// ---------------------------------------------------------------- 1. Đọc dữ liệu
IDataView allData = ml.Data.LoadFromTextFile<IntentInput>(trainFile, separatorChar: ',', hasHeader: true, allowQuoting: true);

// ---------------------------------------------------------------- 2. Chia train / test
var split = ml.Data.TrainTestSplit(allData, testFraction: 0.2, seed: 1);

// ---------------------------------------------------------------- 3. Pipeline
var pipeline = BuildPipeline(ml);

Console.WriteLine("Đang train trên 80% dữ liệu...");
var watch = Stopwatch.StartNew();
ITransformer model = pipeline.Fit(split.TrainSet);
Console.WriteLine($"Xong sau {watch.Elapsed.TotalSeconds:0.0} giây.\n");

// ---------------------------------------------------------------- 4. Đánh giá trên 20% còn lại
var metrics = ml.MulticlassClassification.Evaluate(model.Transform(split.TestSet));
Console.WriteLine("===== KẾT QUẢ TRÊN 20% DỮ LIỆU TÁCH RA =====");
Console.WriteLine($"MicroAccuracy (tỉ lệ câu đoán đúng)        : {metrics.MicroAccuracy:P2}");
Console.WriteLine($"MacroAccuracy (trung bình đều các intent)  : {metrics.MacroAccuracy:P2}");
Console.WriteLine($"LogLoss (càng nhỏ càng tốt)                : {metrics.LogLoss:0.000}");
Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());
Console.WriteLine("Lưu ý: dữ liệu sinh từ câu khung nên con số trên thường rất cao, hãy xem kết quả câu viết tay bên dưới.\n");

// ---------------------------------------------------------------- 5. Câu viết tay
var engine = ml.Model.CreatePredictionEngine<IntentInput, IntentPrediction>(model);
if (File.Exists(handTestFile))
{
    var handData = ml.Data.LoadFromTextFile<IntentInput>(handTestFile, separatorChar: ',', hasHeader: true, allowQuoting: true);
    var handMetrics = ml.MulticlassClassification.Evaluate(model.Transform(handData));
    Console.WriteLine("===== KẾT QUẢ TRÊN CÂU VIẾT TAY (đưa con số này vào báo cáo) =====");
    Console.WriteLine($"Độ chính xác: {handMetrics.MicroAccuracy:P2}");

    foreach (var row in ml.Data.CreateEnumerable<IntentInput>(handData, reuseRowObject: false))
    {
        var p = engine.Predict(row);
        if (p.PredictedLabel != row.Label)
            Console.WriteLine($"  SAI: \"{row.Text}\" | đúng: {row.Label} | đoán: {p.PredictedLabel} ({p.Score.Max():P0})");
    }
    Console.WriteLine();
}

// ---------------------------------------------------------------- 6. Train lại trên toàn bộ dữ liệu rồi lưu
Console.WriteLine("Đang train lại trên 100% dữ liệu để lưu mô hình...");
ITransformer finalModel = BuildPipeline(ml).Fit(allData);
Directory.CreateDirectory(Path.GetDirectoryName(modelFile)!);
ml.Model.Save(finalModel, allData.Schema, modelFile);
Console.WriteLine($"Đã lưu mô hình: {modelFile}\n");

// ---------------------------------------------------------------- 7. Gõ thử
engine = ml.Model.CreatePredictionEngine<IntentInput, IntentPrediction>(finalModel);
var labels = GetLabelNames(engine);
Console.WriteLine("Gõ câu hỏi để thử (Enter trống để thoát):");
while (true)
{
    Console.Write("> ");
    var text = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(text)) break;

    var p = engine.Predict(new IntentInput { Text = text });
    // In 3 intent có điểm cao nhất
    var top3 = p.Score.Select((score, i) => (label: labels[i], score)).OrderByDescending(x => x.score).Take(3);
    Console.WriteLine("  " + string.Join("  |  ", top3.Select(x => $"{x.label}: {x.score:P1}")));
}

// ============================================================================ Hàm phụ
/*
 * Pipeline xử lý:
 *  - MapValueToKey   : đổi tên intent (chữ) thành số để thuật toán dùng
 *  - FeaturizeText   : biến câu thành vector số:
 *                        + n-gram TỪ (1-2 từ liền nhau: "mặc gì", "size gì"...)
 *                        + n-gram KÝ TỰ (1-4 ký tự) -> chịu được lỗi chính tả, viết tắt ("ko", "sz")
 *                        + tự chuyển chữ thường; KeepDiacritics = false: bỏ dấu tiếng Việt
 *                          nên "áo khoác" và "ao khoac" được hiểu giống nhau
 *  - SdcaMaximumEntropy: thuật toán phân loại nhiều lớp (hồi quy logistic), train nhanh, chạy nhẹ
 *  - MapKeyToValue   : đổi số dự đoán ngược lại thành tên intent
 */
static IEstimator<ITransformer> BuildPipeline(MLContext ml)
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

/* Lấy tên intent ứng với từng ô trong mảng Score (để in ra top 3) */
static string[] GetLabelNames(PredictionEngine<IntentInput, IntentPrediction> engine)
{
    VBuffer<ReadOnlyMemory<char>> names = default;
    engine.OutputSchema["Score"].GetSlotNames(ref names);
    return names.DenseValues().Select(x => x.ToString()).ToArray();
}

// ============================================================================ Kiểu dữ liệu
/* 1 dòng trong file CSV. Web (ClothesStore.Api) cũng phải khai báo lớp GIỐNG HỆT để dùng mô hình. */
public class IntentInput
{
    [LoadColumn(0)] public string Text { get; set; } = "";
    [LoadColumn(1)] public string Label { get; set; } = "";
}

/* Kết quả dự đoán */
public class IntentPrediction
{
    [ColumnName("PredictedLabel")] public string PredictedLabel { get; set; } = "";
    public float[] Score { get; set; } = [];   // điểm (xác suất) của từng intent, cộng lại = 1
}