using UnityEngine;

namespace MetalSonic
{
    /// <summary>
    /// Pure ability/resource state machines from the GDD, section 7.
    /// Everything here is deterministic and allocation-free so the headless
    /// test suite can assert exact behaviour without a scene.
    /// </summary>
    public static class MetalSonicMechanics
    {
        // --- 7.3 Teleporte com recarga -------------------------------------
        // "So pode ser utilizado quando a barra esta completamente preenchida."
        // "Um teleporte bem sucedido esvazia a barra, que comeca a se recuperar
        //  gradualmente." "Cada anel coletado adiciona energia a barra."
        public const float TeleportDistance = 1.6f;        // world units, ~10 tiles
        public const float TeleportRegenPerSecond = 0.25f; // 4 s to refill
        public const float TeleportEnergyPerRing = 0.1f;   // 10 rings = full bar

        /// <summary>
        /// Advances the teleport energy bar. Returns the new charge 0..1.
        /// </summary>
        public static float RegenTeleport(float charge, float dt)
        {
            if (charge >= 1f) return 1f;
            return Mathf.Clamp01(charge + TeleportRegenPerSecond * dt);
        }

        /// <summary>Rings top the bar up (GDD 7.3, 7.5).</summary>
        public static float AddRingEnergy(float charge, int rings)
        {
            return Mathf.Clamp01(charge + rings * TeleportEnergyPerRing);
        }

        public static bool CanTeleport(float charge) => charge >= 1f;

        /// <summary>A successful teleport empties the bar.</summary>
        public static float SpendTeleport(float charge)
        {
            return CanTeleport(charge) ? 0f : charge;
        }

        // --- 7.4 Tempus -----------------------------------------------------
        // "O Tempus diminui continuamente durante a jogabilidade e perde uma
        //  parcela adicional em cada troca dimensional."
        // "Capsulas de Estabilidade recuperam parte do tempo, ate um limite maximo."
        public const float TempusMax = 99f;
        public const float TempusStart = 99f;
        public const float TempusDrainPerSecond = 1f;
        public const float TempusSwapCost = 5f;
        public const float TempusCapsuleGain = 10f;

        /// <summary>Continuous drain; pause and cutscenes simply do not call this.</summary>
        public static float DrainTempus(float tempus, float dt)
        {
            return Mathf.Max(0f, tempus - TempusDrainPerSecond * dt);
        }

        /// <summary>Cost charged on a successful dimension swap.</summary>
        public static float ChargeTempusSwap(float tempus)
        {
            return Mathf.Max(0f, tempus - TempusSwapCost);
        }

        public static float ApplyTempusCapsule(float tempus)
        {
            return Mathf.Min(TempusMax, tempus + TempusCapsuleGain);
        }

        public static bool HasTempusForSwap(float tempus) => tempus >= TempusSwapCost;

        // --- 7.5 Aneis e escudo --------------------------------------------
        // "Aneis protegem contra dano e adicionam energia a barra de teleporte."
        // "Monitor de shield ... concede um escudo que absorve um golpe. O escudo
        //  desaparece apos o impacto, preservando os aneis. Coletar outro escudo
        //  nao acumula protecoes."
        public static int RingScatterCount(int rings)
        {
            // classic scatter: at least 1, capped so it stays readable
            return Mathf.Clamp(rings, 0, 32);
        }

        /// <summary>
        /// Resolves one hit. Returns true if the hit was absorbed by a shield.
        /// A shield absorbs exactly one hit and is then gone; rings are kept.
        /// A hit with no shield scatters the rings.
        /// </summary>
        public static bool ResolveHit(int rings, bool hasShield, out int ringsAfter)
        {
            if (hasShield)
            {
                ringsAfter = rings;      // shield preserves rings (GDD 7.5)
                return true;
            }
            ringsAfter = 0;
            return false;
        }

        /// <summary>Shield does not stack (GDD 7.5).</summary>
        public static bool CollectShield(bool hasShield) => true;

        // --- 7.2 Alternancia dimensional ------------------------------------
        // "Se a posicao de chegada estiver ocupada por uma parede solida, a troca
        //  e bloqueada e nenhum tempo adicional e descontado."
        // "Se nao houver tempo suficiente para pagar o custo, a troca nao acontece
        //  e o jogador recebe um aviso."
        public enum SwapResult
        {
            Success,
            BlockedByWall,      // no time deducted
            NotEnoughTempus,    // warning shown
        }

        public static SwapResult TrySwapDimension(
            bool destinationBlocked,
            float tempus,
            out float tempusAfter)
        {
            tempusAfter = tempus;
            if (destinationBlocked) return SwapResult.BlockedByWall;
            if (!HasTempusForSwap(tempus)) return SwapResult.NotEnoughTempus;
            tempusAfter = ChargeTempusSwap(tempus);
            return SwapResult.Success;
        }

        // --- 7.1 Investida (dash) -------------------------------------------
        // "A investida permite derrotar inimigos, mas nao atravessa paredes."
        public const float DashSpeed = 9f;          // px/frame at 60 Hz, faster than top speed
        public const float DashDuration = 0.25f;    // seconds

        public static float DashExitSpeed(float groundSpeed)
        {
            // dash decays back toward normal top speed, keeping some momentum
            return Mathf.Clamp(groundSpeed, -SonicPhysics.TopSpeed, SonicPhysics.TopSpeed);
        }
    }
}
