<script lang="ts">
    import { onMount, tick } from 'svelte';

    /**
     * El nombre del icono a mostrar (sin la extensión .svg).
     * @type {string}
     */
    export let name: string;

    /**
     * Clases CSS adicionales para aplicar al contenedor del icono.
     * @type {string}
     */
    export let className: string = '';

    let svgContent: string | null = null;
    let error: string | null = null;

    // Usamos `onMount` para asegurarnos de que el código se ejecuta solo en el navegador,
    // ya que `import()` con variables no funciona durante el renderizado en servidor (SSR).
    onMount(async () => {
        try {
            // Usamos `import.meta.glob` de Vite para manejar las importaciones dinámicas.
            // Esto le indica a Vite que todos los archivos .svg en esta carpeta son posibles módulos.
            const modules = import.meta.glob('../assets/icons/*.svg', { as: 'raw' });
            const iconName = name.trim().toLowerCase();
            const path = `../assets/icons/${iconName}.svg`;
            
            if (modules[path]) {
                svgContent = await modules[path]();
            } else {
                throw new Error(`Icono '${iconName}' no encontrado en la ruta '${path}'. Módulos disponibles: ${Object.keys(modules).join(', ')}`);
            }
        } catch (e) {
            if (e instanceof Error) {
                error = e.message;
                console.error(error);
            }
        }
    });
</script>

{#if svgContent}
    <i class="icon-wrapper {className}" style="display: inline-block; line-height: 1; width: 1em; height: 1em;">
        {@html svgContent}
    </i>
{:else if error}
    <span class="icon-error" title={error}>⚠️</span>
{:else}
    <!-- Optional: Placeholder while loading, though it's usually instant -->
    <span class="icon-loading"></span>
{/if}