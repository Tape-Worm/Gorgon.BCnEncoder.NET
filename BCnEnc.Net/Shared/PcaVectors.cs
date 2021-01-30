using System;
using DX = SharpDX;
using Gorgon.Graphics;

namespace BCnEncoder.Shared
{
	internal static class PcaVectors
	{
		private const int C565_5_mask = 0xF8;
		private const int C565_6_mask = 0xFC;

		private static void ConvertToVector4(ReadOnlySpan<GorgonColor> colors, Span<DX.Vector4> vectors)
		{
			for (int i = 0; i < colors.Length; i++)
			{
				GorgonColor color = colors[i];
				vectors[i].X += color.Red;
				vectors[i].Y += color.Green;
				vectors[i].Z += color.Blue;
				vectors[i].W += color.Alpha;
			}
		}

		private static void CalculateMean(Span<DX.Vector4> colors, out DX.Vector4 result)
		{

			float r = 0;
			float g = 0;
			float b = 0;
			float a = 0;

			for (int i = 0; i < colors.Length; i++)
			{
				r += colors[i].X;
				g += colors[i].Y;
				b += colors[i].Z;
				a += colors[i].W;
			}

			result = new DX.Vector4(
				r / colors.Length,
				g / colors.Length,
				b / colors.Length,
				a / colors.Length
				);
		}

		internal static void CalculateCovariance(Span<DX.Vector4> values, out DX.Vector4 mean, out DX.Matrix result) {
			CalculateMean(values, out mean);
			for (int i = 0; i < values.Length; i++)
			{
				values[i] -= mean;
			}

			//4x4 matrix
			result = default;

			for (int i = 0; i < values.Length; i++)
			{
				result.M11 += values[i].X * values[i].X;
				result.M12 += values[i].X * values[i].Y;
				result.M13 += values[i].X * values[i].Z;
				result.M14 += values[i].X * values[i].W;

				result.M22 += values[i].Y * values[i].Y;
				result.M23 += values[i].Y * values[i].Z;
				result.M24 += values[i].Y * values[i].W;

				result.M33 += values[i].Z * values[i].Z;
				result.M34 += values[i].Z * values[i].W;

				result.M44 += values[i].W * values[i].W;
			}

			DX.Matrix.Multiply(ref result, 1f / (values.Length - 1), out result);
			
			result.M21 = result.M12;
			result.M31 = result.M13;
			result.M32 = result.M23;
			result.M41 = result.M14;
			result.M42 = result.M24;
			result.M43 = result.M34;
		}

		/// <summary>
		/// Calculate principal axis with the power-method
		/// </summary>
		/// <param name="covarianceMatrix"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		internal static void CalculatePrincipalAxis(ref DX.Matrix covarianceMatrix, out DX.Vector4 result) {
			result = DX.Vector4.UnitY;

			for (int i = 0; i < 30; i++) {
				DX.Vector4.Transform(ref result, ref covarianceMatrix, out DX.Vector4 dA);

				if(dA.LengthSquared() == 0) {
					break;
				}

				dA = DX.Vector4.Normalize(dA);
				DX.Vector4.Dot(ref result, ref dA, out float dot);
				if (dot > 0.999999) {
					result = dA;
					break;
				}
				else {
					result = dA;
				}
			}
		}

		public static void Create(Span<GorgonColor> colors, out DX.Vector3 mean, out DX.Vector3 principalAxis)
		{
			Span<DX.Vector4> vectors = stackalloc DX.Vector4[colors.Length];
			ConvertToVector4(colors, vectors);


            CalculateCovariance(vectors, out DX.Vector4 v4Mean, out DX.Matrix cov);
			mean = new DX.Vector3(v4Mean.X, v4Mean.Y, v4Mean.Z);

            CalculatePrincipalAxis(ref cov, out DX.Vector4 pa);
			principalAxis = new DX.Vector3(pa.X, pa.Y, pa.Z);
			if (principalAxis.LengthSquared() == 0) {
				principalAxis = DX.Vector3.UnitY;
			}
			else {
				principalAxis = DX.Vector3.Normalize(principalAxis);
			}

		}

		public static void CreateWithAlpha(Span<GorgonColor> colors, out DX.Vector4 mean, out DX.Vector4 principalAxis)
		{
			Span<DX.Vector4> vectors = stackalloc DX.Vector4[colors.Length];
			ConvertToVector4(colors, vectors);

            CalculateCovariance(vectors, out mean, out DX.Matrix cov);
			CalculatePrincipalAxis(ref cov, out principalAxis);
		}


		public static void GetExtremePoints(Span<int> colors, DX.Vector3 mean, DX.Vector3 principalAxis, out ColorRgb24 min, out ColorRgb24 max)
		{

			float minD = 0;
			float maxD = 0;

			for (int i = 0; i < colors.Length; i++)
			{
				var colorVec = GorgonColor.FromRGBA(colors[i]).ToVector3();

				DX.Vector3.Subtract(ref colorVec, ref mean, out DX.Vector3 v);
                float d = DX.Vector3.Dot(v, principalAxis);
				if (d < minD)
                {
                    minD = d;
                }

                if (d > maxD)
                {
                    maxD = d;
                }
            }

			DX.Vector3.Multiply(ref principalAxis, minD, out DX.Vector3 minAxis);
			DX.Vector3.Multiply(ref principalAxis, maxD, out DX.Vector3 maxAxis);
			DX.Vector3.Add(ref mean, ref minAxis, out DX.Vector3 minVec);
			DX.Vector3.Add(ref mean, ref maxAxis, out DX.Vector3 maxVec);

			int minR = (int) (minVec.X * 255);
			int minG = (int) (minVec.Y * 255);
			int minB = (int) (minVec.Z * 255);

			int maxR = (int) (maxVec.X * 255);
			int maxG = (int) (maxVec.Y * 255);
			int maxB = (int) (maxVec.Z * 255);

			minR = (minR >= 0) ? minR : 0;
			minG = (minG >= 0) ? minG : 0;
			minB = (minB >= 0) ? minB : 0;

			maxR = (maxR <= 255) ? maxR : 255;
			maxG = (maxG <= 255) ? maxG : 255;
			maxB = (maxB <= 255) ? maxB : 255;

			min = new ColorRgb24((byte)minR, (byte)minG, (byte)minB);
			max = new ColorRgb24((byte)maxR, (byte)maxG, (byte)maxB);
		}

		public static void GetMinMaxColor565(Span<GorgonColor> colors, DX.Vector3 mean, DX.Vector3 principalAxis, 
			out ColorRgb565 min, out ColorRgb565 max)
		{

			float minD = 0;
			float maxD = 0;

			for (int i = 0; i < colors.Length; i++)
			{
				var colorVec = (DX.Vector3)colors[i];

				DX.Vector3.Subtract(ref colorVec, ref mean, out DX.Vector3 v);
				DX.Vector3.Dot(ref v, ref principalAxis, out float d);
				if (d < minD)
                {
                    minD = d;
                }

                if (d > maxD)
                {
                    maxD = d;
                }
            }

			//Inset
			minD *= 15 / 16f;
			maxD *= 15 / 16f;

			DX.Vector3.Multiply(ref principalAxis, minD, out DX.Vector3 minAxis);
			DX.Vector3.Multiply(ref principalAxis, maxD, out DX.Vector3 maxAxis);
			DX.Vector3.Add(ref mean, ref minAxis, out DX.Vector3 minVec);
			DX.Vector3.Add(ref mean, ref maxAxis, out DX.Vector3 maxVec);

			int minR = (int) (minVec.X * 255);
			int minG = (int) (minVec.Y * 255);
			int minB = (int) (minVec.Z * 255);

			int maxR = (int) (maxVec.X * 255);
			int maxG = (int) (maxVec.Y * 255);
			int maxB = (int) (maxVec.Z * 255);

			minR = (minR >= 0) ? minR : 0;
			minG = (minG >= 0) ? minG : 0;
			minB = (minB >= 0) ? minB : 0;

			maxR = (maxR <= 255) ? maxR : 255;
			maxG = (maxG <= 255) ? maxG : 255;
			maxB = (maxB <= 255) ? maxB : 255;

			// Optimal round
			minR = (minR & C565_5_mask) | (minR >> 5);
			minG = (minG & C565_6_mask) | (minG >> 6);
			minB = (minB & C565_5_mask) | (minB >> 5);

			maxR = (maxR & C565_5_mask) | (maxR >> 5);
			maxG = (maxG & C565_6_mask) | (maxG >> 6);
			maxB = (maxB & C565_5_mask) | (maxB >> 5);

			min = new ColorRgb565((byte)minR, (byte)minG, (byte)minB);
			max = new ColorRgb565((byte)maxR, (byte)maxG, (byte)maxB);

		}

		public static void GetExtremePointsWithAlpha(Span<GorgonColor> colors, DX.Vector4 mean, DX.Vector4 principalAxis, out DX.Vector4 min, out DX.Vector4 max)
		{

			float minD = 0;
			float maxD = 0;

			for (int i = 0; i < colors.Length; i++)
			{				
				DX.Vector4 colorVec = colors[i];
				DX.Vector4.Subtract(ref colorVec, ref mean, out DX.Vector4 v);
				DX.Vector4.Dot(ref v, ref principalAxis, out float d);
				if (d < minD)
                {
                    minD = d;
                }

                if (d > maxD)
                {
                    maxD = d;
                }
            }

			DX.Vector4.Multiply(ref principalAxis, minD, out DX.Vector4 axisMin);
			DX.Vector4.Multiply(ref principalAxis, maxD, out DX.Vector4 axisMax);
			DX.Vector4.Add(ref mean, ref axisMin, out min);
			DX.Vector4.Add(ref mean, ref axisMax, out max);
		}

		public static void GetOptimizedEndpoints565(Span<int> colors, DX.Vector3 mean, DX.Vector3 principalAxis, out ColorRgb565 min, out ColorRgb565 max,
			float rWeight = 0.3f, float gWeight = 0.6f, float bWeight = 0.1f)
		{
			int length = colors.Length;
			var vectorColors = new DX.Vector3[length];
			for (int i = 0; i < colors.Length; i++)
			{
				vectorColors[i] = GorgonColor.FromRGBA(colors[i]).ToVector3();
			}

			float minD = 0;
			float maxD = 0;

			void Clamp565(ref DX.Vector3 vec)
			{
				if (vec.X < 0)
                {
                    vec.X = 0;
                }

                if (vec.X > 31)
                {
                    vec.X = 31;
                }

                if (vec.Y < 0)
                {
                    vec.Y = 0;
                }

                if (vec.Y > 63)
                {
                    vec.Y = 63;
                }

                if (vec.Z < 0)
                {
                    vec.Z = 0;
                }

                if (vec.Z > 31)
                {
                    vec.Z = 31;
                }

                vec = new DX.Vector3((float)Math.Round(vec.X), (float)Math.Round(vec.Y), (float)Math.Round(vec.Z));
			}

            void Distance(ref DX.Vector3 v, ref DX.Vector3 p, out float dist) 
				=> dist = (v.X - p.X) * (v.X - p.X) * rWeight
                       + (v.Y - p.Y) * (v.Y - p.Y) * gWeight
                       + (v.Z - p.Z) * (v.Z - p.Z) * bWeight;

            float SelectClosestDistance(ref DX.Vector3 selector, ref DX.Vector3 f0, ref DX.Vector3 f1, ref DX.Vector3 f2, ref DX.Vector3 f3)
			{
				Distance(ref selector, ref f0, out float d0);
				Distance(ref selector, ref f1, out float d1);
				Distance(ref selector, ref f2, out float d2);
				Distance(ref selector, ref f3, out float d3);

                return d0 < d1 && d0 < d2 && d0 < d3 ? d0 : d1 < d0 && d1 < d2 && d1 < d3 ? d1 : d2 < d0 && d2 < d1 && d2 < d3 ? d2 : d3;
            }

            DX.Vector3 endPoint0;
			DX.Vector3 endPoint1;

			double CalculateError()
			{
				double cumulativeError = 0;
				var ep0 = new DX.Vector3(endPoint0.X / 31, endPoint0.Y / 63, endPoint0.Z / 31);
				var ep1 = new DX.Vector3(endPoint1.X / 31, endPoint1.Y / 63, endPoint1.Z / 31);

				DX.Vector3.Subtract(ref ep1, ref ep0, out DX.Vector3 diff);
				DX.Vector3.Divide(ref diff, 3.0f, out DX.Vector3 ep3rds);
				DX.Vector3.Divide(ref diff, 2.0f / 3.0f, out DX.Vector3 ep2_3rds);
				DX.Vector3.Add(ref ep0, ref ep3rds, out DX.Vector3 ep2);
				DX.Vector3.Add(ref ep0, ref ep2_3rds, out DX.Vector3 ep3);

				for (int i = 0; i < length; i++)
				{
					double distance = SelectClosestDistance(ref vectorColors[i], ref ep0, ref ep1, ref ep2, ref ep3);
					cumulativeError += distance;
				}
				return cumulativeError;
			}


			for (int i = 0; i < vectorColors.Length; i++)
			{
				ProjectPointOnLine(ref vectorColors[i], ref mean, ref principalAxis, out float d);
				if (d < minD)
                {
                    minD = d;
                }

                if (d > maxD)
                {
                    maxD = d;
                }
            }

			DX.Vector3.Multiply(ref principalAxis, minD, out DX.Vector3 minAxis);
			DX.Vector3.Multiply(ref principalAxis, maxD, out DX.Vector3 maxAxis);
			DX.Vector3.Add(ref mean, ref minAxis, out endPoint0);
			DX.Vector3.Add(ref mean, ref maxAxis, out endPoint1);

			endPoint0 = new DX.Vector3((float)Math.Round(endPoint0.X * 31), (float)Math.Round(endPoint0.Y * 63), (float)Math.Round(endPoint0.Z * 31));
			endPoint1 = new DX.Vector3((float)Math.Round(endPoint1.X * 31), (float)Math.Round(endPoint1.Y * 63), (float)Math.Round(endPoint1.Z * 31));
			Clamp565(ref endPoint0);
			Clamp565(ref endPoint1);

			double best = CalculateError();
			int increment = 5;
			bool foundBetter = true;
			int rounds = 0;
			// Variate color and look for better endpoints
			while (increment > 1 || foundBetter)
			{
				rounds++;
				foundBetter = false;
				{ // decrement ep0
                    DX.Vector3 prev = endPoint0;

					DX.Vector3.Multiply(ref principalAxis, increment * 2, out DX.Vector3 axisP);
					DX.Vector3.Subtract(ref endPoint0, ref axisP, out endPoint0);
					Clamp565(ref endPoint0);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0 = prev;
					}
				}

				{ // decrement ep1
                    DX.Vector3 prev = endPoint1;

					DX.Vector3.Multiply(ref principalAxis, increment * 2, out DX.Vector3 axisP);
					DX.Vector3.Subtract(ref endPoint1, ref axisP, out endPoint1);

					Clamp565(ref endPoint1);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1 = prev;
					}
				}

				{ // increment ep0
                    DX.Vector3 prev = endPoint0;

					DX.Vector3.Multiply(ref principalAxis, increment * 2, out DX.Vector3 axisP);
					DX.Vector3.Add(ref endPoint0, ref axisP, out endPoint0);

					Clamp565(ref endPoint0);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0 = prev;
					}
				}

				{ // increment ep1
                    DX.Vector3 prev = endPoint1;

					DX.Vector3.Multiply(ref principalAxis, increment * 2, out DX.Vector3 axisP);
					DX.Vector3.Add(ref endPoint1, ref axisP, out endPoint1);

					Clamp565(ref endPoint1);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1 = prev;
					}
				}

				{ // scaleUp 
                    DX.Vector3 prev0 = endPoint0;
                    DX.Vector3 prev1 = endPoint1;

					DX.Vector3.Multiply(ref principalAxis, increment * 2, out DX.Vector3 axisP);
					DX.Vector3.Subtract(ref endPoint0, ref axisP, out endPoint0);
					DX.Vector3.Add(ref endPoint1, ref axisP, out endPoint0);

					Clamp565(ref endPoint0);
					Clamp565(ref endPoint1);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0 = prev0;
						endPoint1 = prev1;
					}
				}

				{ // scaleDown
                    DX.Vector3 prev0 = endPoint0;
                    DX.Vector3 prev1 = endPoint1;

					DX.Vector3.Multiply(ref principalAxis, increment * 2, out DX.Vector3 axisP);
					DX.Vector3.Add(ref endPoint0, ref axisP, out endPoint0);
					DX.Vector3.Subtract(ref endPoint1, ref axisP, out endPoint0);

					Clamp565(ref endPoint0);
					Clamp565(ref endPoint1);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0 = prev0;
						endPoint1 = prev1;
					}
				}

				#region G
				if (endPoint0.Y - increment >= 0)
				{ // decrement ep0 G
					float prevY = endPoint0.Y;
					endPoint0.Y -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.Y = prevY;
					}
				}

				if (endPoint1.Y - increment >= 0)
				{ // decrement ep1 G
					float prevY = endPoint1.Y;
					endPoint1.Y -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.Y = prevY;
					}
				}

				if (foundBetter && increment > 1)
				{
					increment--;
				}

				if (endPoint1.Y + increment <= 63)
				{ // increment ep1 G
					float prevY = endPoint1.Y;
					endPoint1.Y += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.Y = prevY;
					}
				}

				if (endPoint0.Y + increment <= 63)
				{ // increment ep0 G
					float prevY = endPoint0.Y;
					endPoint0.Y += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.Y = prevY;
					}
				}

				#endregion

				#region R
				if (endPoint0.X - increment >= 0)
				{ // decrement ep0 R
					float prevX = endPoint0.X;
					endPoint0.X -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.X = prevX;
					}
				}

				if (endPoint1.X - increment >= 0)
				{ // decrement ep1 R
					float prevX = endPoint1.X;
					endPoint1.X -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.X = prevX;
					}
				}

				if (foundBetter && increment > 1)
				{
					increment--;
				}

				if (endPoint1.X + increment <= 31)
				{ // increment ep1 R
					float prevX = endPoint1.X;
					endPoint1.X += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.X = prevX;
					}
				}

				if (endPoint0.X + increment <= 31)
				{ // increment ep0 R
					float prevX = endPoint0.X;
					endPoint0.X += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.X = prevX;
					}
				}
				#endregion

				#region B

				if (endPoint0.Z - increment >= 0)
				{ // decrement ep0 B
					float prevZ = endPoint0.Z;
					endPoint0.Z -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.Z = prevZ;
					}
				}

				if (endPoint1.Z - increment >= 0)
				{ // decrement ep1 B
					float prevZ = endPoint1.Z;
					endPoint1.Z -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.Z = prevZ;
					}
				}

				if (foundBetter && increment > 1)
				{
					increment--;
				}

				if (endPoint1.Z + increment <= 31)
				{ // increment ep1 B
					float prevZ = endPoint1.Z;
					endPoint1.Z += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.Z = prevZ;
					}
				}

				if (endPoint0.Z + increment <= 31)
				{ // increment ep0 B
					float prevZ = endPoint0.Z;
					endPoint0.Z += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.Z = prevZ;
					}
				}

				#endregion

				Clamp565(ref endPoint0);
				Clamp565(ref endPoint1);

				if (!foundBetter && increment > 1)
				{
					increment--;
				}
			}

            min = new ColorRgb565
            {
                RawR = (int)endPoint0.X,
                RawG = (int)endPoint0.Y,
                RawB = (int)endPoint0.Z
            };

            max = new ColorRgb565
            {
                RawR = (int)endPoint1.X,
                RawG = (int)endPoint1.Y,
                RawB = (int)endPoint1.Z
            };
        }

		private static void ProjectPointOnLine(ref DX.Vector3 point, ref DX.Vector3 linePoint, ref DX.Vector3 lineDir, out float d)
		{
			DX.Vector3.Subtract(ref point, ref linePoint, out DX.Vector3 v);
			DX.Vector3.Dot(ref v, ref lineDir, out d);            
		}
	}
}
