<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'

const props = withDefaults(defineProps<{
  value?: string
  disabled?: boolean
  width?: number
  height?: number
  lineWidth?: number
  lineColor?: string
}>(), {
  value: '',
  disabled: false,
  width: 400,
  height: 200,
  lineWidth: 2,
  lineColor: '#000000',
})

const emit = defineEmits<{
  'update:value': [val: string]
}>()

const canvasRef = ref<HTMLCanvasElement | null>(null)
let ctx: CanvasRenderingContext2D | null = null
let isDrawing = false
const history = ref<ImageData[]>([])
const hasContent = ref(false)

function initCanvas() {
  const canvas = canvasRef.value
  if (!canvas) return
  ctx = canvas.getContext('2d')
  if (!ctx) return
  canvas.width = props.width
  canvas.height = props.height
  ctx.lineCap = 'round'
  ctx.lineJoin = 'round'
  ctx.lineWidth = props.lineWidth
  ctx.strokeStyle = props.lineColor
  // 如果已有签名数据，回显
  if (props.value) {
    const img = new Image()
    img.onload = () => {
      ctx?.drawImage(img, 0, 0)
      hasContent.value = true
    }
    img.src = props.value
  }
}

function getPos(e: MouseEvent | TouchEvent): { x: number; y: number } | null {
  const canvas = canvasRef.value
  if (!canvas) return null
  const rect = canvas.getBoundingClientRect()
  if ('touches' in e) {
    const touch = e.touches[0]
    if (!touch) return null
    return { x: touch.clientX - rect.left, y: touch.clientY - rect.top }
  }
  return { x: e.clientX - rect.left, y: e.clientY - rect.top }
}

function saveState() {
  if (!ctx || !canvasRef.value) return
  history.value.push(ctx.getImageData(0, 0, canvasRef.value.width, canvasRef.value.height))
  // 限制历史记录数量
  if (history.value.length > 30) {
    history.value.shift()
  }
}

function startDraw(e: MouseEvent | TouchEvent) {
  if (props.disabled || !ctx) return
  e.preventDefault()
  isDrawing = true
  saveState()
  const pos = getPos(e)
  if (pos) {
    ctx.beginPath()
    ctx.moveTo(pos.x, pos.y)
  }
}

function draw(e: MouseEvent | TouchEvent) {
  if (!isDrawing || props.disabled || !ctx) return
  e.preventDefault()
  const pos = getPos(e)
  if (pos) {
    ctx.lineTo(pos.x, pos.y)
    ctx.stroke()
  }
}

function endDraw() {
  if (!isDrawing) return
  isDrawing = false
  hasContent.value = true
  emitValue()
}

function emitValue() {
  const canvas = canvasRef.value
  if (!canvas) return
  emit('update:value', canvas.toDataURL('image/png'))
}

function handleUndo() {
  if (!ctx || !canvasRef.value || history.value.length === 0) return
  const prev = history.value.pop()!
  ctx.putImageData(prev, 0, 0)
  hasContent.value = history.value.length > 0 || isCanvasNotEmpty()
  emitValue()
}

function handleClear() {
  if (!ctx || !canvasRef.value) return
  ctx.clearRect(0, 0, canvasRef.value.width, canvasRef.value.height)
  history.value = []
  hasContent.value = false
  emit('update:value', '')
}

function isCanvasNotEmpty(): boolean {
  if (!ctx || !canvasRef.value) return false
  const data = ctx.getImageData(0, 0, canvasRef.value.width, canvasRef.value.height).data
  for (let i = 3; i < data.length; i += 4) {
    if (data[i] !== 0) return true
  }
  return false
}

onMounted(() => {
  initCanvas()
})

onBeforeUnmount(() => {
  ctx = null
  history.value = []
})

watch(() => props.disabled, () => {
  // 禁用时无需特殊处理，事件监听中已判断
})
</script>

<template>
  <div class="signature-input" :class="{ disabled }">
    <canvas
      ref="canvasRef"
      class="signature-canvas"
      :style="{ width: `${width}px`, height: `${height}px`, cursor: disabled ? 'not-allowed' : 'crosshair' }"
      @mousedown="startDraw"
      @mousemove="draw"
      @mouseup="endDraw"
      @mouseleave="endDraw"
      @touchstart="startDraw"
      @touchmove="draw"
      @touchend="endDraw"
    />
    <div v-if="!disabled" class="signature-actions">
      <a-button size="small" :disabled="history.length === 0" @click="handleUndo">
        撤销
      </a-button>
      <a-button size="small" :disabled="!hasContent" @click="handleClear">
        清除
      </a-button>
    </div>
    <div v-if="!hasContent && !disabled" class="signature-placeholder">
      请在此处签名
    </div>
  </div>
</template>

<style scoped>
.signature-input {
  position: relative;
  display: inline-block;
}

.signature-input.disabled {
  opacity: 0.6;
}

.signature-canvas {
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  background: #fff;
  display: block;
}

.signature-actions {
  display: flex;
  gap: 8px;
  margin-top: 8px;
}

.signature-placeholder {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  color: #bfbfbf;
  font-size: 14px;
  pointer-events: none;
  user-select: none;
}
</style>
