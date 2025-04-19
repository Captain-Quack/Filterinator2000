using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;


namespace Filterinator2000.Views;
public partial class MainWindow
{
public static unsafe float DotProduct(float[] vecA, float[] vecB)
    {
        if (vecA == null)
            throw new ArgumentNullException(nameof(vecA));
        if (vecB == null)
            throw new ArgumentNullException(nameof(vecB));
        if (vecA.Length != vecB.Length)
            throw new ArgumentException("Vectors must be the same length.");

        int length = vecA.Length;
        float dot = 0f;

        if (Sse.IsSupported)
        {
            int simdLength = Vector128<float>.Count; // 4 for SSE
            int i = 0;
            Vector128<float> sum = Vector128.Create(0.0f);

            fixed (float* pA = vecA)
            fixed (float* pB = vecB)
            {
                // Process in blocks of 4 floats.
                for (; i <= length - simdLength; i += simdLength)
                {
                    // Load 4 floats from each array.
                    Vector128<float> va = Sse.LoadVector128(pA + i);
                    Vector128<float> vb = Sse.LoadVector128(pB + i);
                    Vector128<float> mul = Sse.Multiply(va, vb);
                    sum = Sse.Add(sum, mul);
                }
            }

            // Horizontally add the elements in the sum vector.
            if (Sse3.IsSupported)
            {
                // Two horizontal add operations collapse the 4 floats into one.
                sum = Sse3.HorizontalAdd(sum, sum);
                sum = Sse3.HorizontalAdd(sum, sum);
                dot = sum.GetElement(0);
            }
            else
            {
                // Manual reduction if Sse3 is not available.
                dot = sum.GetElement(0) + sum.GetElement(1) + sum.GetElement(2) + sum.GetElement(3);
            }

            // Process any remaining elements.
            for (int j = i; j < length; j++)
            {
                dot += vecA[j] * vecB[j];
            }
        }
        else
        {
            // Fallback scalar implementation.
            for (int i = 0; i < length; i++)
            {
                dot += vecA[i] * vecB[i];
            }
        }

        return dot;
    }

    /// <summary>
    /// Computes the Euclidean norm (L2) of a float array using SSE intrinsics if supported.
    /// Falls back to scalar code if not.
    /// </summary>
    public static unsafe float Norm(float[] vec)
    {
        if (vec == null)
            throw new ArgumentNullException(nameof(vec));

        int length = vec.Length;
        float sum = 0f;

        if (Sse.IsSupported)
        {
            int simdLength = Vector128<float>.Count;
            int i = 0;
            Vector128<float> acc = Vector128.Create(0.0f);

            fixed (float* pVec = vec)
            {
                for (; i <= length - simdLength; i += simdLength)
                {
                    Vector128<float> v = Sse.LoadVector128(pVec + i);
                    // Multiply v by itself (element-wise squaring).
                    Vector128<float> square = Sse.Multiply(v, v);
                    acc = Sse.Add(acc, square);
                }
            }

            if (Sse3.IsSupported)
            {
                acc = Sse3.HorizontalAdd(acc, acc);
                acc = Sse3.HorizontalAdd(acc, acc);
                sum = acc.GetElement(0);
            }
            else
            {
                sum = acc.GetElement(0) + acc.GetElement(1) + acc.GetElement(2) + acc.GetElement(3);
            }

            for (int j = i; j < length; j++)
            {
                sum += vec[j] * vec[j];
            }
        }
        else
        {
            for (int i = 0; i < length; i++)
            {
                sum += vec[i] * vec[i];
            }
        }

        return (float)Math.Sqrt(sum);
    }

    /// <summary>
    /// Computes the cosine similarity between two float arrays.
    /// Returns 0 if either vector has zero norm.
    /// </summary>
    public static float CosineSimilarity(float[] vecA, float[] vecB)
    {
        float dot = DotProduct(vecA, vecB);
        float normA = Norm(vecA);
        float normB = Norm(vecB);

        if (normA == 0 || normB == 0)
            return 0f;

        return dot / (normA * normB);
    }

}