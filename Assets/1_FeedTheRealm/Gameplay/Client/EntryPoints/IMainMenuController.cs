using System;

/// <summary>
/// Contrato para el panel de menú principal (world feed).
/// Al implementarlo, el componente puede ser usado por ClientEntryPoint
/// sin crear dependencia circular entre ensamblados.
/// </summary>
public interface IMainMenuController
{
    /// <summary>
    /// Se dispara cuando el usuario seleccionó un mundo y está listo para cargar.
    /// </summary>
    event Action OnNavigateToWorld;
}
