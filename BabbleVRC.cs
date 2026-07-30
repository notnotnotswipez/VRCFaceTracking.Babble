using System.Reflection;
using Microsoft.Extensions.Logging;
using PSVR2Toolkit;
using PSVR2Toolkit.VRCFT;
using VRCFaceTracking.Baballonia;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.Core.Types;

namespace VRCFaceTracking.Babble;

public class BabbleVRC : ExtTrackingModule
{
    private BabbleOsc babbleOSC;

    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);

    private const int k_noiseFilterSamples = 15;
    private LowPassFilter? m_leftEyeOpenLowPass;
    private LowPassFilter? m_rightEyeOpenLowPass;

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        Config babbleConfig = BabbleConfig.GetBabbleConfig();
        babbleOSC = new BabbleOsc(iLogger: Logger, babbleConfig.Host, babbleConfig.Port);
        List<Stream> list = new List<Stream>();
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("VRCFaceTracking.Babble.BabbleLogo.png")!;
        list.Add(manifestResourceStream);

        var result = PSVR2ToolkitCAPI.Init();
        if (result != 0)
        {
            Logger.LogWarning("PSVR2ToolKit failed to initialize.");
            return (false, false);
        }


        m_leftEyeOpenLowPass = new LowPassFilter(k_noiseFilterSamples);
        m_rightEyeOpenLowPass = new LowPassFilter(k_noiseFilterSamples);

        ModuleInformation = new ModuleMetadata
        {
            Name = "Project Babble Module (PSVR2ToolKit Modified) v3.1.0",
            StaticImages = list
        };
        return (true, true);
    }

    public override void Teardown()
    {
        babbleOSC.Teardown();
        PSVR2ToolkitCAPI.Deinit();
    }

    public override void Update()
    {
        if (Status == ModuleState.Active)
        {
            // LEFT EYE
            UnifiedTracking.Data.Shapes[(int)UnifiedExpressions.EyeWideLeft].Weight =
                BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeLeftWiden];

            UnifiedTracking.Data.Shapes[(int)UnifiedExpressions.EyeSquintLeft].Weight =
                BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeLeftSquint];

            // BROW
            UnifiedTracking.Data.Shapes[(int)UnifiedExpressions.BrowLowererLeft].Weight =
                BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeLeftLower];

            UnifiedTracking.Data.Shapes[(int)UnifiedExpressions.BrowPinchLeft].Weight =
                BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeLeftLower];
            

            // RIGHT EYE
            UnifiedTracking.Data.Shapes[(int)UnifiedExpressions.EyeWideRight].Weight =
                BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeRightWiden];

            UnifiedTracking.Data.Shapes[(int)UnifiedExpressions.EyeSquintRight].Weight =
                BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeRightSquint];

            // BROW
            UnifiedTracking.Data.Shapes[(int)UnifiedExpressions.BrowLowererRight].Weight =
                BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeRightLower];

            UnifiedTracking.Data.Shapes[(int)UnifiedExpressions.BrowPinchRight].Weight =
                BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeRightLower];

            hmd2_gaze_status_t gazeStatus = new hmd2_gaze_status_t();

            if (!PSVR2ToolkitCAPI.GetGazeStatus(ref gazeStatus, 1000))
            {
                return;
            }

            if (gazeStatus.wearable.left.is_blink_valid == hmd2_gaze_bool_t.HMD2_GAZE_BOOL_TRUE)
            {
                float leftOpenness = gazeStatus.wearable.left.blink == hmd2_gaze_bool_t.HMD2_GAZE_BOOL_TRUE ? 0 : 1;

                if (m_leftEyeOpenLowPass != null)
                {
                    leftOpenness = m_leftEyeOpenLowPass.FilterValue(leftOpenness);
                }

                if (leftOpenness < 0.9f)
                {
                    UnifiedTracking.Data.Eye.Left.Openness = leftOpenness;
                }
                else {
                    // Fall to babbles openness
                    UnifiedTracking.Data.Eye.Left.Openness = BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeLeftLid];
                }
            }

            if (gazeStatus.wearable.right.is_blink_valid == hmd2_gaze_bool_t.HMD2_GAZE_BOOL_TRUE)
            {
                float rightOpenness = gazeStatus.wearable.right.blink == hmd2_gaze_bool_t.HMD2_GAZE_BOOL_TRUE ? 0 : 1;

                if (m_rightEyeOpenLowPass != null)
                {
                    rightOpenness = m_rightEyeOpenLowPass.FilterValue(rightOpenness);
                }

                if (rightOpenness < 0.9f)
                {
                    UnifiedTracking.Data.Eye.Right.Openness = rightOpenness;
                }
                else {
                    // Fall to babbles openness
                    UnifiedTracking.Data.Eye.Right.Openness = BabbleOsc.EyeExpressions[(int)ExpressionMapping.EyeRightLid];
                }
            }

            if (gazeStatus.wearable.left.is_gaze_dir_valid == hmd2_gaze_bool_t.HMD2_GAZE_BOOL_TRUE)
            {
                UnifiedTracking.Data.Eye.Left.Gaze = new Vector2(gazeStatus.wearable.left.gaze_dir_norm.x, gazeStatus.wearable.left.gaze_dir_norm.y).FlipXCoordinates();
            }

            if (gazeStatus.wearable.right.is_gaze_dir_valid == hmd2_gaze_bool_t.HMD2_GAZE_BOOL_TRUE)
            {
                UnifiedTracking.Data.Eye.Right.Gaze = new Vector2(gazeStatus.wearable.right.gaze_dir_norm.x, gazeStatus.wearable.right.gaze_dir_norm.y).FlipXCoordinates();
            }

            if (gazeStatus.wearable.left.is_pupil_dia_valid == hmd2_gaze_bool_t.HMD2_GAZE_BOOL_TRUE)
            {
                UnifiedTracking.Data.Eye.Left.PupilDiameter_MM = gazeStatus.wearable.left.pupil_dia_mm;
            }
            if (gazeStatus.wearable.right.is_pupil_dia_valid == hmd2_gaze_bool_t.HMD2_GAZE_BOOL_TRUE)
            {
                UnifiedTracking.Data.Eye.Right.PupilDiameter_MM = gazeStatus.wearable.right.pupil_dia_mm;
            }

            // Force the normalization values of Dilation to fit avg. pupil values.
            UnifiedTracking.Data.Eye._minDilation = 0;
            UnifiedTracking.Data.Eye._maxDilation = 10;
        }

        Thread.Sleep(10);

        
    }
}
