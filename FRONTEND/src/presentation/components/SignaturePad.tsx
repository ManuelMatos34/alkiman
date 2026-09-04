import { forwardRef, useImperativeHandle, useRef, useState, type ComponentProps } from "react"
import SignatureCanvas from "react-signature-canvas"
import { RotateCcw, Check } from "lucide-react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

/** Extrae el base64 puro de una data URL `data:image/png;base64,...` (el backend lo espera sin el prefijo). */
function stripDataUrlPrefix(dataUrl: string) {
  return dataUrl.replace(/^data:image\/\w+;base64,/, "")
}

interface SignaturePadProps extends ComponentProps<"div"> {
  /** Se invoca con el PNG en base64 (sin prefijo) al confirmar la firma dibujada. */
  onSave: (base64Png: string) => void
}

/** Handle imperativo expuesto por ref para forzar la sincronización desde el padre (ver `trySave`). */
export interface SignaturePadHandle {
  /**
   * Intenta guardar la firma actual del lienzo aunque ningún evento de la librería haya
   * disparado el autoguardado todavía. Es la red de seguridad usada justo antes de enviar
   * el formulario: no dependemos únicamente de `onEnd`/`onBegin` de react-signature-canvas.
   * Devuelve el base64 (sin prefijo) si había un trazo, o `null` si el lienzo está vacío.
   */
  trySave: () => string | null
}

/** Lienzo para capturar una firma digital manuscrita y exportarla como PNG en base64. */
export const SignaturePad = forwardRef<SignaturePadHandle, SignaturePadProps>(function SignaturePad(
  { onSave, className, ...props },
  ref
) {
  const padRef = useRef<SignatureCanvas>(null)
  const [isEmpty, setIsEmpty] = useState(true)
  const [saved, setSaved] = useState(false)

  function handleClear() {
    padRef.current?.clear()
    setIsEmpty(true)
    setSaved(false)
  }

  function trySave(): string | null {
    const pad = padRef.current
    if (!pad || pad.isEmpty()) return null

    // Nota: evitamos getTrimmedCanvas() a propósito — depende del paquete transitivo
    // `trim-canvas`, que bajo el pre-bundling de Vite en dev tira
    // "TypeError: (0, import_build.default) is not a function" y rompe TODO guardado de
    // firma (manual, automático, o el forzado desde el submit). toDataURL() sin recortar
    // evita esa dependencia rota; solo se pierde el recorte cosmético del margen en blanco.
    const dataUrl = pad.toDataURL("image/png")
    const base64 = stripDataUrlPrefix(dataUrl)
    onSave(base64)
    setSaved(true)
    return base64
  }

  function handleSave() {
    trySave()
  }

  useImperativeHandle(ref, () => ({ trySave }))

  /**
   * Respaldo independiente de `onEnd`: algunos navegadores/dispositivos no disparan el
   * callback interno de la librería de forma confiable (orden de eventos entre el
   * listener nativo del canvas y React). Escuchamos el fin del trazo directamente sobre
   * el contenedor y difirimos la lectura un tick para asegurarnos de que el trazo ya
   * quedó registrado en el pad antes de guardar.
   */
  function handleStrokeEndFallback() {
    setTimeout(handleSave, 0)
  }

  return (
    <div className={cn("space-y-2", className)} {...props}>
      <div
        className="overflow-hidden rounded-lg border border-border/60 bg-muted/40"
        onMouseUp={handleStrokeEndFallback}
        onTouchEnd={handleStrokeEndFallback}
        onPointerUp={handleStrokeEndFallback}
      >
        <SignatureCanvas
          ref={padRef}
          penColor="black"
          canvasProps={{ className: "h-40 w-full touch-none" }}
          onBegin={() => {
            setIsEmpty(false)
            setSaved(false)
          }}
          onEnd={handleSave}
        />
      </div>
      <div className="flex items-center justify-between gap-2">
        <p className="text-xs text-muted-foreground">
          {saved
            ? "Firma guardada."
            : "Firmá con el mouse o el dedo dentro del recuadro."}
        </p>
        <div className="flex gap-2">
          <Button type="button" variant="outline" size="sm" onClick={handleClear}>
            <RotateCcw className="h-4 w-4" />
            Limpiar
          </Button>
          <Button type="button" size="sm" disabled={isEmpty} onClick={handleSave}>
            <Check className="h-4 w-4" />
            Guardar firma
          </Button>
        </div>
      </div>
    </div>
  )
})
